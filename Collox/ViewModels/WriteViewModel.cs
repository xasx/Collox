using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Media;
using System.Speech.Synthesis;
using Collox.Models;
using Collox.Services;
using Collox.ViewModels.Messages;
using CommunityToolkit.Mvvm.Collections;
using CommunityToolkit.Mvvm.Messaging;
using EmojiToolkit;
using Markdig;
using Microsoft.Extensions.AI;
using NLog;
using Windows.ApplicationModel;
using Windows.System;

namespace Collox.ViewModels;

public partial class WriteViewModel : ObservableObject, ITitleBarAutoSuggestBoxAware, IRecipient<TaskDoneMessage>
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    // Add this method to cache the markdown pipeline
    private static readonly Lazy<MarkdownPipeline> _markdownPipeline = new(
        () => new MarkdownPipelineBuilder().UseAdvancedExtensions().Build());

    // Improve voice initialization with lazy loading
    private static readonly Lazy<ICollection<VoiceInfo>> _voiceInfos = new(
        () => [.. new SpeechSynthesizer().GetInstalledVoices().Select(iv => iv.VoiceInfo)]);

    private readonly IAIService aiService;
    private readonly IStoreService storeService;

    public WriteViewModel(IStoreService storeService, IAIService aiService)
    {
        Logger.Info("Initializing WriteViewModel");

        this.storeService = storeService;
        this.aiService = aiService;

        try
        {
            Logger.Debug("Initializing AI service");
            aiService.Init();
            Logger.Info("AI service initialized successfully");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to initialize AI service. Disabling AI.");
            Settings.EnableAI = false;
        }

        Tasks.CollectionChanged += (_, _) =>
        {
            ShowTasks = Tasks.Count > 0;
            Logger.Debug(
                "Tasks collection changed. ShowTasks: {ShowTasks}, TaskCount: {TaskCount}",
                ShowTasks,
                Tasks.Count);
        };

        WeakReferenceMessenger.Default.RegisterAll(this);
        Logger.Debug("Registered for all weak reference messages");

        // Update the lambda expression to use the adapter
        MessageRelativeTimeUpdater.CreateTimer = () =>
        {
            var dispatcherQueueTimer = DispatcherQueue.GetForCurrentThread().CreateTimer();
            return new DispatcherQueueTimerAdapter(dispatcherQueueTimer);
        };

        Logger.Info("WriteViewModel initialization completed");
    }

    partial void OnConversationContextChanged(TabData value)
    { Logger.Debug("ConversationContext changed to {ContextName}", value?.Context ?? "unknown"); }

    private async Task AddMore(TextColloxMessage textColloxMessage)
    {
        Logger.Info("Starting further processing for message: {MessageId}", textColloxMessage.GetHashCode());

        if (!Settings.EnableAI)
        {
            Logger.Debug("AI is disabled, skipping further processing");
            textColloxMessage.IsLoading = false;
            return;
        }

        var processorCount = ConversationContext.ActiveProcessors.Count;
        Logger.Debug("Processing with {ProcessorCount} active processors", processorCount);

        try
        {
            var procs = ConversationContext.ActiveProcessors;
            var tasks = procs.Select(
                async processor =>
                {
                    Logger.Debug(
                        "Processing with processor: {ProcessorName} (ID: {ProcessorId})",
                        processor.Name,
                        processor.Id);

                    try
                    {
                        processor.Process = async (client) => processor.Target switch
                        {
                            Target.Comment => await CreateComment(textColloxMessage, processor, client)
                                .ConfigureAwait(false),
                            Target.Task => await CreateTask(textColloxMessage, processor, client)
                                .ConfigureAwait(false),
                            Target.Message => await ModifyMessage(textColloxMessage, processor, client)
                                .ConfigureAwait(false),
                            Target.Chat => await CreateMessage(Messages.OfType<TextColloxMessage>(), processor, client)
                                .ConfigureAwait(false),
                            _ => throw new NotImplementedException($"Target {processor.Target} not implemented"),
                        };
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
                        Logger.Error(
                            ex,
                            "Exception in processor {ProcessorName}: {ErrorMessage}",
                            processor.Name,
                            ex.Message);
                        textColloxMessage.ErrorMessage = $"Error: {ex.Message}";
                        textColloxMessage.HasProcessingError = true;
                    }
                });

            await Task.WhenAll(tasks).ConfigureAwait(true);
            Logger.Info("Completed further processing for all {ProcessorCount} processors", processorCount);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Critical error in further processing: {ErrorMessage}", ex.Message);
            textColloxMessage.ErrorMessage = $"Error: {ex.Message}";
            textColloxMessage.HasProcessingError = true;
        }

        textColloxMessage.IsLoading = false;
        Logger.Debug("Set IsLoading to false for message after further processing");
    }

    private async Task<string> ModifyMessage(
        TextColloxMessage textColloxMessage,
        IntelligentProcessor processor,
        IChatClient client)
    {
        Logger.Info("Modifying message with processor: {ProcessorName}", processor.Name);

        var originalText = textColloxMessage.Text;
        textColloxMessage.Text = string.Empty;

        try
        {
            await foreach (var update in client.GetStreamingResponseAsync(string.Format(processor.Prompt, originalText)))
            {
                textColloxMessage.Text += update.Text;
            }

            Logger.Debug(
                "Message modification completed. Original length: {OriginalLength}, New length: {NewLength}",
                originalText.Length,
                textColloxMessage.Text.Length);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error during message modification with processor {ProcessorName}", processor.Name);
            textColloxMessage.Text = originalText; // Fallback to original text
            throw;
        }

        return textColloxMessage.Text;
    }

    private async Task AddTextMessage()
    {
        Logger.Info("Adding new text message. Input length: {InputLength}", InputMessage.Length);

        var textMessage = new TextColloxMessage
        {
            Text = InputMessage,
            Timestamp = DateTime.Now,
            IsLoading = true,
            Context = ConversationContext.Context
        };

        Messages.Add(textMessage);
        Logger.Debug("Added message to collection. Total messages: {MessageCount}", Messages.Count);

        var originalInput = InputMessage;
        InputMessage = string.Empty;

        CharacterCount = Math.Min(KeyStrokesCount, CharacterCount + textMessage.Text.Length);
        Logger.Debug("Updated CharacterCount to {CharacterCount}", CharacterCount);

        if (Settings.PersistMessages)
        {
            try
            {
                Logger.Debug("Persisting message to store");
                var singleMessage = new SingleMessage(textMessage.Text, textMessage.Context, textMessage.Timestamp);
                await storeService.Append(singleMessage).ConfigureAwait(true);
                Logger.Debug("Message persisted successfully");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to persist message to store");
            }
        }

        WeakReferenceMessenger.Default.Send(new TextSubmittedMessage(textMessage));
        Logger.Debug("Sent TextSubmittedMessage");

        // Execute beep and speak operations asynchronously and concurrently
        var audioTasks = new List<Task>();

        if (IsBeeping)
        {
            Logger.Debug("Playing beep sound asynchronously");
            audioTasks.Add(PlayBeepSoundAsync());
        }

        if (IsSpeaking)
        {
            Logger.Debug("Reading text with voice: {VoiceName}", SelectedVoice?.Name ?? "Default");
            audioTasks.Add(ReadTextAsync(textMessage.Text, SelectedVoice?.Name));
        }

        if (audioTasks.Count > 0)
        {
            try
            {
                await Task.WhenAll(audioTasks).ConfigureAwait(true);
                Logger.Debug("Completed all audio operations");
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "One or more audio operations failed");
            }
        }

        await AddMore(textMessage).ConfigureAwait(false);
        Logger.Info("Completed adding text message");
    }

    private static async Task PlayBeepSoundAsync()
    {
        try
        {
            await Task.Run(() =>
            {
                var installedPath = Package.Current.InstalledLocation.Path;
                var sp = new SoundPlayer(Path.Combine(installedPath, "Assets", "notify.wav"));
                sp.PlaySync(); // Use PlaySync for proper awaiting
            }).ConfigureAwait(false);
            Logger.Debug("Beep sound played successfully");
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Failed to play beep sound");
        }
    }

    internal static async Task ReadTextAsync(string text, string voice = null)
    {
        Logger.Debug("Reading text with voice: {Voice}, TextLength: {Length}", voice ?? "Default", text.Length);

        try
        {
            await Task.Run(() =>
            {
                using var speechSynthesizer = new SpeechSynthesizer();
                speechSynthesizer.SetOutputToDefaultAudioDevice();

                if (voice == null)
                {
                    speechSynthesizer.SelectVoiceByHints(VoiceGender.Male, VoiceAge.Adult);
                }
                else
                {
                    speechSynthesizer.SelectVoice(voice);
                }

                speechSynthesizer.Speak(text); // Use synchronous Speak for proper awaiting
            }).ConfigureAwait(false);
            Logger.Debug("Text reading completed successfully");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to read text with voice: {Voice}", voice);
            Debug.WriteLine(ex);
        }
    }

    [RelayCommand]
    public async Task SpeakLastAsync()
    {
        Logger.Debug("SpeakLast command initiated");

        if (Messages.Count > 0)
        {
            var lastTextMessage = Messages.OfType<TextColloxMessage>().LastOrDefault();
            if (lastTextMessage != null)
            {
                var textToSpeak = StripMd(lastTextMessage.Text);
                await ReadTextAsync(textToSpeak, SelectedVoice?.Name).ConfigureAwait(false);
                Logger.Debug("Speaking last message with length: {Length}", textToSpeak.Length);
            }
            else
            {
                Logger.Debug("No text messages found to speak");
            }
        }
        else
        {
            Logger.Debug("No messages available to speak");
        }
    }

    private static async Task<string> CreateComment(
        TextColloxMessage textColloxMessage,
        IntelligentProcessor processor,
        IChatClient client)
    {
        Logger.Info("Creating comment with processor: {ProcessorName}", processor.Name);

        var comment = new ColloxMessageComment() { Comment = string.Empty, GeneratorId = processor.Id, };
        textColloxMessage.Comments.Add(comment);

        try
        {
            await foreach (var update in client.GetStreamingResponseAsync(
                string.Format(processor.Prompt, textColloxMessage.Text)))
            {
                comment.Comment += update.Text;
            }

            Logger.Debug("Comment created successfully. Length: {CommentLength}", comment.Comment.Length);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error creating comment with processor {ProcessorName}", processor.Name);
            throw;
        }

        return comment.Comment;
    }

    private async Task<string> CreateMessage(
        IEnumerable<TextColloxMessage> messages,
        IntelligentProcessor processor,
        IChatClient client)
    {
        Logger.Info("Creating chat message with processor: {ProcessorName}", processor.Name);

        var textColloxMessage = new TextColloxMessage
        {
            Text = string.Empty,
            Timestamp = DateTime.Now,
            IsLoading = true,
            IsGenerated = true,
            GeneratorId = processor.Id,
            Context = ConversationContext.Context
        };
        Messages.Add(textColloxMessage);

        var chatMessages = new List<ChatMessage> { new(ChatRole.System, processor.SystemPrompt) };

        foreach (var message in messages)
        {
            if (message.IsGenerated)
            {
                if (message.GeneratorId == processor.Id)
                {
                    chatMessages.Add(new ChatMessage(ChatRole.Assistant, message.Text));
                }
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(message.Text))
                {
                    chatMessages.Add(new ChatMessage(ChatRole.User, message.Text));
                }
            }
        }

        Logger.Debug(
            "Built chat context with {MessageCount} messages for processor {ProcessorName}",
            chatMessages.Count,
            processor.Name);

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

    private async Task<string> CreateTask(TextColloxMessage textColloxMessage, IntelligentProcessor processor, IChatClient client)
    {
        Logger.Info("Creating task from message");

        try
        {
            var response = await client.GetResponseAsync(string.Format(processor.Prompt, textColloxMessage.Text))
                .ConfigureAwait(true);

            Tasks.Add(new TaskViewModel { Name = response.Text, IsDone = false });

            Logger.Debug("Task created: {TaskName}", response.Text);
            return response.Text;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error creating task from message");
            throw;
        }
    }

    partial void OnIsBeepingChanged(bool value)
    {
        Logger.Debug("IsBeeping changed to {Value}", value);
        Settings.AutoBeep = value;
    }

    partial void OnIsSpeakingChanged(bool value)
    {
        Logger.Debug("IsSpeaking changed to {Value}", value);
        Settings.AutoRead = value;
    }

    partial void OnSelectedMessageChanged(ColloxMessage value)
    {
        Logger.Debug("SelectedMessage changed");
        // always scroll to the selected message
        WeakReferenceMessenger.Default.Send(new MessageSelectedMessage(value));
    }

    partial void OnSelectedVoiceChanged(VoiceInfo value)
    {
        Logger.Debug("SelectedVoice changed to {VoiceName}", value?.Name ?? "null");
        Settings.Voice = value.Name;
    }

    private async Task ProcessCommand()
    {
        var msg = InputMessage;
        Logger.Info("Processing command: {Command}", msg);

        InputMessage = string.Empty;
        var tok = msg.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        switch (tok)
        {
            case ["clear", ..]:
                Logger.Debug("Executing clear command");
                await Clear().ConfigureAwait(false);
                return;

            case ["save", ..]:
                Logger.Debug("Executing save command");
                await SaveNow().ConfigureAwait(false);
                return;

            case ["speak", ..]:
                Logger.Debug("Executing speak command");
                await SpeakLastAsync();
                return;

            case ["time", ..]:
                Logger.Debug("Executing time command");
                var timestampMessage = new TimeColloxMessage { Time = DateTime.Now.TimeOfDay };
                Messages.Add(timestampMessage);
                return;

            case ["pin", ..]:
                Logger.Debug("Executing pin command");
                ConversationContext.IsCloseable = false;
                return;

            case ["unpin", ..]:
                Logger.Debug("Executing unpin command");
                ConversationContext.IsCloseable = true;
                return;

            case ["help", ..]:
                Logger.Debug("Executing help command");
                var helpMessage = new InternalColloxMessage
                {
                    Message = "Available commands: clear, save, speak, time, pin, unpin, task",
                    Severity = InfoBarSeverity.Informational
                };
                Messages.Add(helpMessage);
                return;

            case ["task", .. var taskName]:
                var taskNameStr = string.Join(" ", taskName);
                Logger.Debug("Executing task command: {TaskName}", taskNameStr);
                Tasks.Add(new TaskViewModel { Name = taskNameStr, IsDone = false });
                return;

            default:
                Logger.Warn("Unknown command: {Command}", msg);
                break;
        }
    }

    [RelayCommand]
    private async Task SaveNow()
    {
        Logger.Info("Save command initiated");

        try
        {
            await storeService.SaveNow().ConfigureAwait(false);
            Logger.Info("Save completed successfully");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Save operation failed");
        }
    }

    private static string StripMd(string mdText)
    {
        try
        {
            return Markdown.ToPlainText(mdText, _markdownPipeline.Value);
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Failed to strip markdown from text: {Text}", mdText);
            return mdText; // Return original text if markdown processing fails
        }
    }

    [RelayCommand]
    private async Task Submit()
    {
        if (string.IsNullOrWhiteSpace(InputMessage))
        {
            Logger.Debug("Submit called with empty input message");
            return;
        }

        Logger.Info(
            "Submit command initiated. Mode: {Mode}, InputLength: {Length}",
            SubmitModeIcon,
            InputMessage.Length);

        ConversationContext.IsEditing = false;

        try
        {
            switch (SubmitModeIcon)
            {
                case Symbol.Send:
                    Logger.Debug("Processing as text message");
                    await AddTextMessage().ConfigureAwait(false);
                    break;
                case Symbol.Play:
                    Logger.Debug("Processing as command");
                    await ProcessCommand().ConfigureAwait(false);
                    break;
                default:
                    Logger.Warn("Unknown submit mode: {Mode}", SubmitModeIcon);
                    break;
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error during submit processing");
        }
    }

    private static CultureInfo DetectLanguage(string text)
    {
        Logger.Debug("Detecting language for text of length: {Length}", text.Length);

        try
        {
            var inputModel = new LanguageDetection.ModelInput() { Text = text };
            var lang = LanguageDetection.Predict(inputModel);
            var ll = lang.PredictedLabel.Trim('"');

            var le = ll switch
            {
                "english" => "en-US",
                "french" => "fr-FR",
                "german" => "de-DE",
                "spanish" => "es-ES",
                "italian" => "it-IT",
                "portuguese" => "pt-PT",
                "chinese" => "zh-CN",
                "japanese" => "ja-JP",
                "korean" => "ko-KR",
                "russian" => "ru-RU",
                "arabic" => "ar-SA",
                "dutch" => "nl-NL",
                "polish" => "pl-PL",
                "turkish" => "tr-TR",
                "hindi" => "hi-IN",
                "swedish" => "sv-SE",
                "norwegian" => "no-NO",
                "danish" => "da-DK",
                "finnish" => "fi-FI",
                "czech" => "cs-CZ",
                "hungarian" => "hu-HU",
                "greek" => "el-GR",
                _ => "en-US"
            };

            var culture = CultureInfo.GetCultureInfoByIetfLanguageTag(le);
            Logger.Debug("Language detected: {Language} -> {Culture}", ll, culture.Name);
            return culture;
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Language detection failed, defaulting to en-US");
            return CultureInfo.GetCultureInfoByIetfLanguageTag("en-US");
        }
    }

    [RelayCommand]
    public void ChangeModeToCmd()
    {
        Logger.Debug("Changing mode to Command");
        SubmitModeIcon = Symbol.Play;
    }

    [RelayCommand]
    public void ChangeModeToWrite()
    {
        Logger.Debug("Changing mode to Write");
        SubmitModeIcon = Symbol.Send;
    }

    [RelayCommand]
    public async Task Clear()
    {
        Logger.Info("Clear command initiated. Current message count: {MessageCount}", Messages.Count);

        try
        {
            Messages.Clear();
            await storeService.SaveNow().ConfigureAwait(false);
            Logger.Info("Clear completed successfully");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Clear operation failed");
        }
    }

    public void OnAutoSuggestBoxQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        Logger.Debug("AutoSuggestBox query submitted: {Query}", args.QueryText);

        var message = Messages
            .OfType<TextColloxMessage>()
            .FirstOrDefault(p => p.Text == args.QueryText);
        if (message != null)
        {
            SelectedMessage = message;
            Logger.Debug("Selected message found and set");
        }
        else
        {
            Logger.Debug("No matching message found for query");
        }
    }

    public void OnAutoSuggestBoxTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        Logger.Debug("AutoSuggestBox text changed: {Text}", sender.Text);

        try
        {
            var suggestions = Messages
                .OfType<TextColloxMessage>()
                .Where(p => p.Text.Contains(sender.Text))
                .Select(p => p.Text)
                .ToArray();

            AutoSuggestBoxHelper.LoadSuggestions(sender, args, [.. suggestions]);
            Logger.Debug("Loaded {SuggestionCount} suggestions", suggestions.Length);
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Failed to load suggestions for AutoSuggestBox");
        }
    }

    public void Receive(TaskDoneMessage message)
    {
        Logger.Debug("Received TaskDoneMessage for task: {TaskName}", message.Value.Name);
        Tasks.Remove(message.Value);
        Logger.Debug("Task removed from collection. Remaining tasks: {TaskCount}", Tasks.Count);
    }

    [RelayCommand]
    public void SwitchMode()
    {
        Logger.Debug("SwitchMode command initiated. Current mode: {Mode}", SubmitModeIcon);

        if (SubmitModeIcon == Symbol.Send)
        {
            ChangeModeToCmd();
        }
        else
        {
            ChangeModeToWrite();
        }

        Logger.Debug("Mode switched to: {Mode}", SubmitModeIcon);
    }

    public List<IntelligentProcessorViewModel> AvailableProcessors { get; init; } = [];

    [ObservableProperty] public partial int CharacterCount { get; set; }

    [ObservableProperty] public partial bool ClockShown { get; set; }

    [ObservableProperty] public partial TabData ConversationContext { get; set; }

    public ObservableGroupedCollection<string, EmojiRecord> Emojis
    {
        get;
        init;
    }
        = new ObservableGroupedCollection<string, EmojiRecord>(Emoji.All.GroupBy(e => e.Category));

    [ObservableProperty] public partial string Filename { get; set; }

    [ObservableProperty] public partial string InputMessage { get; set; } = string.Empty;

    public ICollection<VoiceInfo> InstalledVoices => _voiceInfos.Value;

    [ObservableProperty] public partial bool IsBeeping { get; set; } = Settings.AutoBeep;

    [ObservableProperty] public partial bool IsSpeaking { get; set; } = Settings.AutoRead;

    [ObservableProperty] public partial int KeyStrokesCount { get; set; }

    [ObservableProperty] public partial ObservableCollection<ColloxMessage> Messages { get; set; } = [];

    [ObservableProperty] public partial ColloxMessage SelectedMessage { get; set; }

    [ObservableProperty]
    public partial VoiceInfo SelectedVoice
    {
        get;
        set;
    }
        = _voiceInfos.Value.FirstOrDefault(vi => vi.Name == Settings.Voice);

    [ObservableProperty] public partial bool ShowTasks { get; set; }

    [ObservableProperty] public partial Symbol SubmitModeIcon { get; set; } = Symbol.Send;

    [ObservableProperty] public partial ObservableCollection<TaskViewModel> Tasks { get; set; } = [];
}
