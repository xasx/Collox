using Collox.Models;
using Microsoft.Extensions.AI;
using Serilog;

namespace Collox.Services;

public class MessageProcessingService : IMessageProcessingService
{
    private static readonly ILogger Logger = Log.ForContext<MessageProcessingService>();
    private readonly IMcpService mcpService;

    public MessageProcessingService(IMcpService mcpService)
    {
        this.mcpService = mcpService;
    }

    public async Task ProcessMessageAsync(MessageProcessingContext context,
        IEnumerable<IntelligentProcessor> processors)
    {
        Logger.Information("Starting processing for message: {MessageId}", context.CurrentMessage.GetHashCode());

        if (!Settings.EnableAI)
        {
            Logger.Debug("AI is disabled, skipping processing");
            context.CurrentMessage.IsLoading = false;
            return;
        }

        var intelligentProcessors = processors.ToList();
        var processorCount = intelligentProcessors.Count;
        Logger.Debug("Processing with {ProcessorCount} active processors", processorCount);

        try
        {
            var tasks = intelligentProcessors.Select(async processor =>
            {
                Logger.Debug("Processing with processor: {ProcessorName} (ID: {ProcessorId})", processor.Name,
                    processor.Id);

                try
                {
                    processor.OnError = (ex) =>
                    {
                        Logger.Error(ex, "Processor {ProcessorName} encountered an error", processor.Name);
                        context.CurrentMessage.ErrorMessage = $"Error: {ex.Message}";
                        context.CurrentMessage.HasProcessingError = true;
                    };

                    Logger.Debug("Starting work for processor: {ProcessorName}", processor.Name);
                    await processor.Work(context).ConfigureAwait(false);
                    Logger.Debug("Completed work for processor: {ProcessorName}", processor.Name);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Exception in processor {ProcessorName}: {ErrorMessage}", processor.Name,
                        ex.Message);
                    context.CurrentMessage.ErrorMessage = $"Error: {ex.Message}";
                    context.CurrentMessage.HasProcessingError = true;
                }
            });

            await Task.WhenAll(tasks).ConfigureAwait(true);
            Logger.Information("Completed processing for all {ProcessorCount} processors", processorCount);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Critical error in further processing: {ErrorMessage}", ex.Message);
            context.CurrentMessage.ErrorMessage = $"Error: {ex.Message}";
            context.CurrentMessage.HasProcessingError = true;
        }

        context.CurrentMessage.IsLoading = false;
    }

    public async Task<string> CreateCommentAsync(MessageProcessingContext context, IntelligentProcessor processor,
        IChatClient client)
    {
        Logger.Information("Creating comment with processor: {ProcessorName}", processor.Name);

        var comment = new ColloxMessageComment() { Comment = string.Empty, GeneratorId = processor.Id };
        context.CurrentMessage.Comments.Add(comment);

        try
        {
            await StreamResponseAsync(
                client,
                processor,
                context.CurrentMessage.Text,
                text => comment.Comment += text);

            Logger.Debug("Comment created successfully. Length: {CommentLength}", comment.Comment.Length);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error creating comment with processor {ProcessorName}", processor.Name);
            throw;
        }

        return comment.Comment;
    }

    public async Task<string> CreateTaskAsync(MessageProcessingContext context, IntelligentProcessor processor,
        IChatClient client)
    {
        Logger.Information("Creating task from message");

        try
        {
            var response = await GetSingleResponseAsync(client, processor, context.CurrentMessage.Text);
            if (string.IsNullOrWhiteSpace(response))
            {
                Logger.Debug("Received empty response when trying to get task");
                return string.Empty;
            }

            context.Tasks.Add(new TaskViewModel { Name = response, IsDone = false });
            Logger.Debug("Task created: {TaskNameLength}", response.Length);
            return response;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error creating task from message");
            throw;
        }
    }

    public async Task<string> ModifyMessageAsync(MessageProcessingContext context, IntelligentProcessor processor,
        IChatClient client)
    {
        Logger.Information("Modifying message with processor: {ProcessorName}", processor.Name);

        var originalText = context.CurrentMessage.Text;
        context.CurrentMessage.Text = string.Empty;

        try
        {
            await StreamResponseAsync(
                client,
                processor,
                originalText,
                text => context.CurrentMessage.Text += text);

            Logger.Debug("Message modification completed. Original length: {OriginalLength}, New length: {NewLength}",
                originalText.Length, context.CurrentMessage.Text.Length);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error during message modification with processor {ProcessorName}", processor.Name);
            context.CurrentMessage.Text = originalText;
            throw;
        }

        return context.CurrentMessage.Text;
    }

    public async Task<string> CreateChatMessageAsync(MessageProcessingContext context, IntelligentProcessor processor,
        IChatClient client)
    {
        Logger.Information("Creating chat message with processor: {ProcessorName}", processor.Name);

        var textColloxMessage = new TextColloxMessage
        {
            Text = string.Empty,
            Timestamp = DateTime.Now,
            IsLoading = true,
            IsGenerated = true,
            GeneratorId = processor.Id,
            Context = context.Context
        };
        context.Messages.Add(textColloxMessage);

        var chatMessages = BuildChatContext(context.Messages.OfType<TextColloxMessage>(), processor);

        try
        {
            var tools = await mcpService.GetTools();
            await foreach (var update in client.GetStreamingResponseAsync(chatMessages, new ChatOptions()
                           {
                               ToolMode = ChatToolMode.Auto,
                               Tools = [.. tools]
                           }))
            {
                textColloxMessage.Text += update.Text;
            }

            Logger.Debug("Chat message generation completed. Length: {MessageLength}", textColloxMessage.Text.Length);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error generating chat message with processor {ProcessorName}", processor.Name);
            throw;
        }

        textColloxMessage.IsLoading = false;
        return textColloxMessage.Text;
    }

    private async Task StreamResponseAsync(IChatClient client, IntelligentProcessor processor, string inputText,
        Action<string> onTextReceived)
    {
        var chatMessages = new List<ChatMessage>();

        if (!string.IsNullOrWhiteSpace(processor.SystemPrompt))
        {
            chatMessages.Add(new ChatMessage(ChatRole.System, processor.SystemPrompt));
        }

        var userMessage = new ChatMessage(ChatRole.User, string.Format(processor.Prompt, inputText));
        chatMessages.Add(userMessage);

        var tools = await mcpService.GetTools();
        await foreach (var update in client.GetStreamingResponseAsync(chatMessages, new ChatOptions()
                       {
                           ToolMode = ChatToolMode.Auto,
                           Tools = [.. tools]
                       }))
        {
            onTextReceived(update.Text);
        }
    }

    private async Task<string> GetSingleResponseAsync(IChatClient client, IntelligentProcessor processor,
        string inputText)
    {
        var chatMessages = new List<ChatMessage>();

        if (!string.IsNullOrWhiteSpace(processor.SystemPrompt))
        {
            chatMessages.Add(new ChatMessage(ChatRole.System, processor.SystemPrompt));
        }

        var userMessage = new ChatMessage(ChatRole.User, string.Format(processor.Prompt, inputText));
        chatMessages.Add(userMessage);

        var tools = await mcpService.GetTools();
        var response = await client.GetResponseAsync(chatMessages, new ChatOptions()
        {
            ToolMode = ChatToolMode.Auto,
            Tools = [.. tools]
        }).ConfigureAwait(true);
        return response.Text;
    }

    private List<ChatMessage> BuildChatContext(IEnumerable<TextColloxMessage> messages, IntelligentProcessor processor)
    {
        var chatMessages = new List<ChatMessage>();

        if (!string.IsNullOrWhiteSpace(processor.SystemPrompt))
        {
            chatMessages.Add(new ChatMessage(ChatRole.System, processor.SystemPrompt));
        }

        var messageCount = 0;
        foreach (var message in messages)
        {
            if (message.IsGenerated)
            {
                if (message.GeneratorId == processor.Id)
                {
                    chatMessages.Add(new ChatMessage(ChatRole.Assistant, message.Text));
                    messageCount++;
                }
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(message.Text))
                {
                    chatMessages.Add(new ChatMessage(ChatRole.User, message.Text));
                    messageCount++;
                }
            }
        }

        Logger.Debug("Built chat context with {MessageCount} messages for processor {ProcessorName}", messageCount,
            processor.Name);
        return chatMessages;
    }
}
