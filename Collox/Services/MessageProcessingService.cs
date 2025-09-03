using System.Collections.ObjectModel;
using Collox.Models;
using Microsoft.Extensions.AI;
using Serilog;

namespace Collox.Services;

public class MessageProcessingService : IMessageProcessingService
{
    private static readonly ILogger Logger = Log.ForContext<MessageProcessingService>();

    public async Task ProcessMessageAsync(TextColloxMessage textColloxMessage, IEnumerable<IntelligentProcessor> processors)
    {
        Logger.Information("Starting further processing for message: {MessageId}", textColloxMessage.GetHashCode());

        if (!Settings.EnableAI)
        {
            Logger.Debug("AI is disabled, skipping further processing");
            textColloxMessage.IsLoading = false;
            return;
        }

        var processorCount = processors.Count();
        Logger.Debug("Processing with {ProcessorCount} active processors", processorCount);

        try
        {
            var tasks = processors.Select(async processor =>
            {
                Logger.Debug("Processing with processor: {ProcessorName} (ID: {ProcessorId})", processor.Name, processor.Id);

                try
                {
                    processor.OnError = (ex) =>
                    {
                        Logger.Error(ex, "Processor {ProcessorName} encountered an error", processor.Name);
                        textColloxMessage.ErrorMessage = $"Error: {ex.Message}";
                        textColloxMessage.HasProcessingError = true;
                    };

                    Logger.Debug("Starting work for processor: {ProcessorName}", processor.Name);
                    await processor.Work().ConfigureAwait(false);
                    Logger.Debug("Completed work for processor: {ProcessorName}", processor.Name);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Exception in processor {ProcessorName}: {ErrorMessage}", processor.Name, ex.Message);
                    textColloxMessage.ErrorMessage = $"Error: {ex.Message}";
                    textColloxMessage.HasProcessingError = true;
                }
            });

            await Task.WhenAll(tasks).ConfigureAwait(true);
            Logger.Information("Completed further processing for all {ProcessorCount} processors", processorCount);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Critical error in further processing: {ErrorMessage}", ex.Message);
            textColloxMessage.ErrorMessage = $"Error: {ex.Message}";
            textColloxMessage.HasProcessingError = true;
        }

        textColloxMessage.IsLoading = false;
    }

    public async Task<string> CreateCommentAsync(TextColloxMessage textColloxMessage, IntelligentProcessor processor, IChatClient client)
    {
        Logger.Information("Creating comment with processor: {ProcessorName}", processor.Name);

        var comment = new ColloxMessageComment() { Comment = string.Empty, GeneratorId = processor.Id };
        textColloxMessage.Comments.Add(comment);

        try
        {
            await StreamResponseAsync(
                client,
                processor,
                textColloxMessage.Text,
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

    public async Task<string> CreateTaskAsync(TextColloxMessage textColloxMessage, IntelligentProcessor processor, IChatClient client, ObservableCollection<TaskViewModel> tasks)
    {
        Logger.Information("Creating task from message");

        try
        {
            var response = await GetSingleResponseAsync(client, processor, textColloxMessage.Text);
            tasks.Add(new TaskViewModel { Name = response, IsDone = false });
            Logger.Debug("Task created: {TaskName}", response);
            return response;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error creating task from message");
            throw;
        }
    }

    public async Task<string> ModifyMessageAsync(TextColloxMessage textColloxMessage, IntelligentProcessor processor, IChatClient client)
    {
        Logger.Information("Modifying message with processor: {ProcessorName}", processor.Name);

        var originalText = textColloxMessage.Text;
        textColloxMessage.Text = string.Empty;

        try
        {
            await StreamResponseAsync(
                client,
                processor,
                originalText,
                text => textColloxMessage.Text += text);

            Logger.Debug("Message modification completed. Original length: {OriginalLength}, New length: {NewLength}",
                originalText.Length, textColloxMessage.Text.Length);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error during message modification with processor {ProcessorName}", processor.Name);
            textColloxMessage.Text = originalText;
            throw;
        }

        return textColloxMessage.Text;
    }

    public async Task<string> CreateChatMessageAsync(IEnumerable<TextColloxMessage> messages, IntelligentProcessor processor, IChatClient client, ObservableCollection<ColloxMessage> messagesCollection, string context)
    {
        Logger.Information("Creating chat message with processor: {ProcessorName}", processor.Name);

        var textColloxMessage = new TextColloxMessage
        {
            Text = string.Empty,
            Timestamp = DateTime.Now,
            IsLoading = true,
            IsGenerated = true,
            GeneratorId = processor.Id,
            Context = context
        };
        messagesCollection.Add(textColloxMessage);

        var chatMessages = BuildChatContext(messages, processor);

        try
        {
            await foreach (var update in client.GetStreamingResponseAsync(chatMessages))
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

    private async Task StreamResponseAsync(IChatClient client, IntelligentProcessor processor, string inputText, Action<string> onTextReceived)
    {
        var chatMessages = new List<ChatMessage>();

        if (!string.IsNullOrWhiteSpace(processor.SystemPrompt))
        {
            chatMessages.Add(new ChatMessage(ChatRole.System, processor.SystemPrompt));
        }

        var userMessage = new ChatMessage(ChatRole.User, string.Format(processor.Prompt, inputText));
        chatMessages.Add(userMessage);

        await foreach (var update in client.GetStreamingResponseAsync(chatMessages))
        {
            onTextReceived(update.Text);
        }
    }

    private async Task<string> GetSingleResponseAsync(IChatClient client, IntelligentProcessor processor, string inputText)
    {
        var chatMessages = new List<ChatMessage>();

        if (!string.IsNullOrWhiteSpace(processor.SystemPrompt))
        {
            chatMessages.Add(new ChatMessage(ChatRole.System, processor.SystemPrompt));
        }

        var userMessage = new ChatMessage(ChatRole.User, string.Format(processor.Prompt, inputText));
        chatMessages.Add(userMessage);

        var response = await client.GetResponseAsync(chatMessages).ConfigureAwait(true);
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

        Logger.Debug("Built chat context with {MessageCount} messages for processor {ProcessorName}", messageCount, processor.Name);
        return chatMessages;
    }
}
