using Collox.Models;
using Microsoft.Extensions.AI;
using Serilog;

namespace Collox.Services;

public class MessageProcessingService : IMessageProcessingService, IDisposable
{
    private static readonly ILogger Logger = Log.ForContext<MessageProcessingService>();
    private readonly Task<IMcpService> mcpServiceTask;
    private IMcpService _mcpService;
    private int _disposed;

    public MessageProcessingService(Task<IMcpService> mcpServiceTask)
    {
        this.mcpServiceTask = mcpServiceTask;
    }

    private async ValueTask<IMcpService> GetMcpServiceAsync()
    {
        if (_mcpService is not null)
        {
            return _mcpService;
        }

        try
        {
            var service = await mcpServiceTask.ConfigureAwait(false);
            Interlocked.CompareExchange(ref _mcpService, service, null);
            return _mcpService;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to initialize MCP service. MCP-dependent operations will be unavailable");
            throw;
        }
    }

    public async Task ProcessMessageAsync(MessageProcessingContext context,
        IEnumerable<IntelligentProcessor> processors, CancellationToken cancellationToken = default)
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
                    await processor.Work(context, cancellationToken).ConfigureAwait(false);
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
        IChatClient client, CancellationToken cancellationToken = default)
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
                text => comment.Comment += text,
                cancellationToken);

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
        IChatClient client, CancellationToken cancellationToken = default)
    {
        Logger.Information("Creating task from message");

        try
        {
            var response = await GetSingleResponseAsync(client, processor, context.CurrentMessage.Text, cancellationToken);
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
        IChatClient client, CancellationToken cancellationToken = default)
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
                text => context.CurrentMessage.Text += text,
                cancellationToken);

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
        IChatClient client, CancellationToken cancellationToken = default)
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
            var tools = await (await GetMcpServiceAsync()).GetTools(cancellationToken);
            await foreach (var update in client.GetStreamingResponseAsync(chatMessages, new ChatOptions()
                           {
                               ToolMode = ChatToolMode.Auto,
                               Tools = [.. tools]
                           }, cancellationToken))
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
        Action<string> onTextReceived, CancellationToken cancellationToken = default)
    {
        var chatMessages = new List<ChatMessage>();

        if (!string.IsNullOrWhiteSpace(processor.SystemPrompt))
        {
            chatMessages.Add(new ChatMessage(ChatRole.System, processor.SystemPrompt));
        }

        var userMessage = new ChatMessage(ChatRole.User, string.Format(processor.Prompt, inputText));
        chatMessages.Add(userMessage);

        var tools = await (await GetMcpServiceAsync()).GetTools(cancellationToken);
        await foreach (var update in client.GetStreamingResponseAsync(chatMessages, new ChatOptions()
                       {
                           ToolMode = ChatToolMode.Auto,
                           Tools = [.. tools]
                       }, cancellationToken))
        {
            onTextReceived(update.Text);
        }
    }

    private async Task<string> GetSingleResponseAsync(IChatClient client, IntelligentProcessor processor,
        string inputText, CancellationToken cancellationToken = default)
    {
        var chatMessages = new List<ChatMessage>();

        if (!string.IsNullOrWhiteSpace(processor.SystemPrompt))
        {
            chatMessages.Add(new ChatMessage(ChatRole.System, processor.SystemPrompt));
        }

        var userMessage = new ChatMessage(ChatRole.User, string.Format(processor.Prompt, inputText));
        chatMessages.Add(userMessage);

        var tools = await (await GetMcpServiceAsync()).GetTools(cancellationToken);
        var response = await client.GetResponseAsync(chatMessages, new ChatOptions()
        {
            ToolMode = ChatToolMode.Auto,
            Tools = [.. tools]
        }, cancellationToken).ConfigureAwait(true);
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

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        Logger.Debug("Disposing MessageProcessingService");

        // McpService is a DI-managed singleton; let the container handle its disposal.

        Logger.Information("MessageProcessingService disposed successfully");
    }
}
