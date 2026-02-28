using System.Collections.ObjectModel;
using System.Speech.Synthesis;
using Collox.Models;
using Collox.Services;
using Collox.ViewModels.Messages;
using CommunityToolkit.Mvvm.Collections;
using CommunityToolkit.Mvvm.Messaging;
using EmojiToolkit;
using Markdig;
using Serilog;
using Windows.System;

namespace Collox.ViewModels;

public partial class WriteViewModel : ObservableObject, ITitleBarAutoSuggestBoxAware, IRecipient<TaskDoneMessage>, IDisposable
{
    private static readonly ILogger Logger = Log.ForContext<WriteViewModel>();

    private static readonly Lazy<MarkdownPipeline> _markdownPipeline =
        new(() => new MarkdownPipelineBuilder().UseAdvancedExtensions().Build());

    private readonly IStoreService storeService;
    private readonly IAudioService audioService;
    private readonly IMessageProcessingService messageProcessingService;
    private readonly ICommandService commandService;
    private bool _disposed;

    public WriteViewModel(
        IStoreService storeService,
        IAudioService audioService,
        IMessageProcessingService messageProcessingService,
        ICommandService commandService)
    {
        Logger.Information("Initializing WriteViewModel");

        this.storeService = storeService;
        this.audioService = audioService;
        this.messageProcessingService = messageProcessingService;
        this.commandService = commandService;

        SetupEventHandlers();
        ConfigureMessaging();


        Logger.Information("WriteViewModel initialization completed");
    }

    private void SetupEventHandlers()
    {
        Tasks.CollectionChanged += (_, _) =>
        {
            ShowTasks = Tasks.Count > 0;
            Logger.Debug("Tasks collection changed. ShowTasks: {ShowTasks}, TaskCount: {TaskCount}", ShowTasks,
                Tasks.Count);
        };

        MessageRelativeTimeUpdater.CreateTimer = () =>
        {
            var dispatcherQueueTimer = DispatcherQueue.GetForCurrentThread().CreateTimer();
            return new DispatcherQueueTimerAdapter(dispatcherQueueTimer);
        };
    }

    private void ConfigureMessaging()
    {
        WeakReferenceMessenger.Default.RegisterAll(this);
        Logger.Debug("Registered for all weak reference messages");
    }

    // Properties
    [ObservableProperty]
    public partial ObservableCollection<IntelligentProcessorViewModel> AvailableProcessors { get; set; } =
        [];

    [ObservableProperty] public partial int CharacterCount { get; set; }
    [ObservableProperty] public partial bool ClockShown { get; set; }
    [ObservableProperty] public partial TabData ConversationContext { get; set; }

    public ObservableGroupedCollection<string, EmojiRecord> Emojis { get; init; } =
        new ObservableGroupedCollection<string, EmojiRecord>(Emoji.All.GroupBy(e => e.Category));

    [ObservableProperty] public partial string Filename { get; set; }
    [ObservableProperty] public partial int HitPercentage { get; set; }
    [ObservableProperty] public partial string InputMessage { get; set; } = string.Empty;
    public ICollection<VoiceInfo> InstalledVoices => audioService.GetInstalledVoices();
    [ObservableProperty] public partial bool IsBeeping { get; set; }
    [ObservableProperty] public partial bool IsSpeaking { get; set; }
    [ObservableProperty] public partial int KeyStrokesCount { get; set; }
    [ObservableProperty] public partial ObservableCollection<ColloxMessage> Messages { get; set; } = [];
    [ObservableProperty] public partial ColloxMessage SelectedMessage { get; set; }
    [ObservableProperty] public partial VoiceInfo SelectedVoice { get; set; }
    [ObservableProperty] public partial bool ShowTasks { get; set; }
    [ObservableProperty] public partial Symbol SubmitModeIcon { get; set; } = Symbol.Send;
    [ObservableProperty] public partial ObservableCollection<TaskViewModel> Tasks { get; set; } = [];

    // Property Change Handlers
    partial void OnConversationContextChanged(TabData value)
    {
        IsBeeping = value.IsBeeping;
        IsSpeaking = value.IsSpeaking;
        var installedVoices = audioService.GetInstalledVoices();
        SelectedVoice = installedVoices.FirstOrDefault(vi => vi.Name == value.SelectedVoice)
            ?? installedVoices.FirstOrDefault();

        Logger.Debug("ConversationContext changed to {ContextName}", value.Context ?? "unknown");
    }

    partial void OnIsBeepingChanged(bool value)
    {
        Logger.Debug("IsBeeping changed to {Value}", value);
        ConversationContext.IsBeeping = value;
        WeakReferenceMessenger.Default.Send(new UpdateTabMessage(ConversationContext));
    }

    partial void OnIsSpeakingChanged(bool value)
    {
        Logger.Debug("IsSpeaking changed to {Value}", value);
        ConversationContext.IsSpeaking = value;
        WeakReferenceMessenger.Default.Send(new UpdateTabMessage(ConversationContext));
    }

    partial void OnSelectedMessageChanged(ColloxMessage value)
    {
        Logger.Debug("SelectedMessage changed");
        WeakReferenceMessenger.Default.Send(new MessageSelectedMessage(value));
    }

    partial void OnSelectedVoiceChanged(VoiceInfo value)
    {
        Logger.Debug("SelectedVoice changed to {VoiceName}", value?.Name ?? "null");
        if (value != null)
        {
            ConversationContext.SelectedVoice = value.Name;
            WeakReferenceMessenger.Default.Send(new UpdateTabMessage(ConversationContext));
        }
    }

    // Commands
    [RelayCommand]
    private async Task Submit()
    {
        if (string.IsNullOrWhiteSpace(InputMessage))
        {
            Logger.Debug("Submit called with empty input message");
            return;
        }

        Logger.Information("Submit command initiated. Mode: {Mode}, InputLength: {Length}", SubmitModeIcon,
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
                    Logger.Warning("Unknown submit mode: {Mode}", SubmitModeIcon);
                    break;
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error during submit processing");
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
                await audioService.ReadTextAsync(textToSpeak, SelectedVoice?.Name).ConfigureAwait(false);
                Logger.Debug("Speaking last message with length: {Length}", textToSpeak.Length);
            }
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
    public void SwitchMode()
    {
        Logger.Debug("SwitchMode command initiated. Current mode: {Mode}", SubmitModeIcon);
        if (SubmitModeIcon == Symbol.Send)
            ChangeModeToCmd();
        else
            ChangeModeToWrite();
        Logger.Debug("Mode switched to: {Mode}", SubmitModeIcon);
    }

    [RelayCommand]
    public async Task Clear()
    {
        Logger.Information("Clear command initiated. Current message count: {MessageCount}", Messages.Count);

        try
        {
            Messages.Clear();
            await storeService.SaveNow(CancellationToken.None).ConfigureAwait(false);
            Logger.Information("Clear completed successfully");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Clear operation failed");
        }
    }

    [RelayCommand]
    private async Task SaveNow()
    {
        Logger.Information("Save command initiated");

        try
        {
            await storeService.SaveNow(CancellationToken.None).ConfigureAwait(false);
            Logger.Information("Save completed successfully");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Save operation failed");
        }
    }

    // Private Methods
    private async Task AddTextMessage()
    {
        Logger.Information("Adding new text message. Input length: {InputLength}", InputMessage.Length);

        var textMessage = new TextColloxMessage
        {
            Text = InputMessage,
            Timestamp = DateTime.Now,
            IsLoading = true,
            Context = ConversationContext.Context
        };

        Messages.Add(textMessage);
        Logger.Debug("Added message to collection. Total messages: {MessageCount}", Messages.Count);

        InputMessage = string.Empty;

        CharacterCount += textMessage.Text.Length;
        Logger.Debug("Updated CharacterCount to {CharacterCount}", CharacterCount);
        UpdateHitPercentage();

        await PersistMessageIfEnabled(textMessage);
        SendTextSubmittedMessage(textMessage);
        await ExecuteAudioOperations(textMessage.Text);
        await ProcessMessageWithAI(textMessage);

        Logger.Information("Completed adding text message");
    }

    public void UpdateHitPercentage()
    {
        HitPercentage = KeyStrokesCount == 0 ? 0 : (int)((double)CharacterCount / KeyStrokesCount * 100);
        Logger.Debug("Updated HitPercentage to {HitPercentage}%", HitPercentage);
    }

    private async Task PersistMessageIfEnabled(TextColloxMessage textMessage)
    {
        if (Settings.PersistMessages)
        {
            try
            {
                Logger.Debug("Persisting message to store");
                var singleMessage = new SingleMessage(textMessage.Text, textMessage.Context, textMessage.Timestamp);
                await storeService.Append(singleMessage, CancellationToken.None).ConfigureAwait(true);
                Logger.Debug("Message persisted successfully");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to persist message to store");
            }
        }
    }

    private void SendTextSubmittedMessage(TextColloxMessage textMessage)
    {
        WeakReferenceMessenger.Default.Send(new TextSubmittedMessage(textMessage));
        Logger.Debug("Sent TextSubmittedMessage");
    }

    private async Task ExecuteAudioOperations(string text)
    {
        var audioTasks = new List<Task>();

        if (IsBeeping)
        {
            Logger.Debug("Playing beep sound asynchronously");
            audioTasks.Add(audioService.PlayBeepSoundAsync(CancellationToken.None));
        }

        if (IsSpeaking)
        {
            Logger.Debug("Reading text with voice: {VoiceName}", SelectedVoice?.Name ?? "Default");
            audioTasks.Add(audioService.ReadTextAsync(text, SelectedVoice?.Name, CancellationToken.None));
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
                Logger.Warning(ex, "One or more audio operations failed");
            }
        }
    }

    private async Task ProcessMessageWithAI(TextColloxMessage textMessage)
    {
        // Set up processor configurations for the new service
        foreach (var processor in ConversationContext.ActiveProcessors)
        {
            processor.Process ??= processor.Target switch
            {
                Target.Comment => messageProcessingService.CreateCommentAsync,
                Target.Task => messageProcessingService.CreateTaskAsync,
                Target.Message => messageProcessingService.ModifyMessageAsync,
                Target.Chat => messageProcessingService.CreateChatMessageAsync,
                _ => throw new NotImplementedException($"Target {processor.Target} not implemented"),
            };
        }

        await messageProcessingService.ProcessMessageAsync(new MessageProcessingContext(textMessage,
                Messages,
                ConversationContext.Context,
                Tasks),
            ConversationContext.ActiveProcessors, CancellationToken.None);
    }

    private async Task ProcessCommand()
    {
        var command = InputMessage;
        InputMessage = string.Empty;

        var commandContext = new CommandContext
        {
            Messages = Messages,
            Tasks = Tasks,
            ConversationContext = ConversationContext,
            StoreService = storeService,
            AudioService = audioService
        };

        var result = await commandService.ProcessCommandAsync(command, commandContext, CancellationToken.None);

        if (result.ResultMessage != null)
        {
            Messages.Add(result.ResultMessage);
        }

        if (!result.Success && !string.IsNullOrEmpty(result.ErrorMessage))
        {
            Logger.Warning("Command failed: {Error}", result.ErrorMessage);
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
            Logger.Warning(ex, "Failed to strip markdown from text: {Text}", mdText);
            return mdText;
        }
    }

    // Interface Implementations
    public void OnAutoSuggestBoxQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        Logger.Debug("AutoSuggestBox query submitted: {Query}", args.QueryText);

        var message = Messages.OfType<TextColloxMessage>().FirstOrDefault(p => p.Text == args.QueryText);
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
            Logger.Warning(ex, "Failed to load suggestions for AutoSuggestBox");
        }
    }

    public void Receive(TaskDoneMessage message)
    {
        Logger.Debug("Received TaskDoneMessage for task: {TaskName}", message.Value.Name);
        Tasks.Remove(message.Value);
        Logger.Debug("Task removed from collection. Remaining tasks: {TaskCount}", Tasks.Count);
    }

    // Keep this static method for backward compatibility with existing code
    internal static async Task ReadTextAsync(string text, string voice = null)
    {
        // This method is kept for backward compatibility but should be deprecated
        // New code should use IAudioService instead
        var audioService = App.GetService<IAudioService>();
        await audioService.ReadTextAsync(text, voice, CancellationToken.None);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        Logger.Debug("Disposing WriteViewModel");
        WeakReferenceMessenger.Default.UnregisterAll(this);
        _disposed = true;
        Logger.Information("WriteViewModel disposed successfully");
    }
}
