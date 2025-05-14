using System.Collections.ObjectModel;
using System.Media;
using System.Speech.Synthesis;
using Collox.Models;
using Collox.Services;
using CommunityToolkit.Mvvm.Collections;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using EmojiToolkit;
using Markdig;
using Microsoft.Extensions.AI;
using Windows.ApplicationModel;

namespace Collox.ViewModels;

public partial class WriteViewModel : ObservableObject, ITitleBarAutoSuggestBoxAware, IRecipient<TaskDoneMessage>
{
    private static readonly ICollection<VoiceInfo> voiceInfos =
        [.. new SpeechSynthesizer().GetInstalledVoices().Select(iv => iv.VoiceInfo)];

    private readonly IStoreService storeService = App.GetService<IStoreService>();
    private readonly AIService aiService = App.GetService<AIService>();

    public WriteViewModel()
    {
        aiService.Init();
        Tasks.CollectionChanged += (_, _) => ShowTasks = Tasks.Count > 0;
        WeakReferenceMessenger.Default.RegisterAll(this);
    }

    [ObservableProperty] public partial int CharacterCount { get; set; }

    [ObservableProperty] public partial TabData ConversationContext { get; set; }

    public ICollection<VoiceInfo> InstalledVoices => voiceInfos;

    [ObservableProperty] public partial bool IsBeeping { get; set; } = Settings.AutoBeep;

    [ObservableProperty] public partial bool IsSpeaking { get; set; } = Settings.AutoRead;

    [ObservableProperty] public partial int KeyStrokesCount { get; set; }

    [ObservableProperty] public partial string InputMessage { get; set; } = string.Empty;

    [ObservableProperty] public partial ObservableCollection<ColloxMessage> Messages { get; set; } = [];

    [ObservableProperty] public partial ColloxMessage SelectedMessage { get; set; }

    [ObservableProperty]
    public partial VoiceInfo SelectedVoice { get; set; }
        = voiceInfos.FirstOrDefault(vi => vi.Name == Settings.Voice);

    [ObservableProperty] public partial Symbol SubmitModeIcon { get; set; } = Symbol.Send;

    [ObservableProperty] public partial string Filename { get; set; }

    [ObservableProperty] public partial bool ClockShown { get; set; }

    public List<IntelligentProcessorViewModel> AvailableProcessors { get; init; } = [];

    public ObservableGroupedCollection<string, EmojiRecord> Emojis { get; init; }
        = new ObservableGroupedCollection<string, EmojiRecord>(Emoji.All.GroupBy(e => e.Category));

    [ObservableProperty] public partial ObservableCollection<TaskViewModel> Tasks { get; set; } = [];
    [ObservableProperty] public partial bool ShowTasks { get; set; }

    public void OnAutoSuggestBoxTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        AutoSuggestBoxHelper.LoadSuggestions(sender, args, [
            .. Messages
                .OfType<TextColloxMessage>()
                .Where(p => p.Text.Contains(sender.Text)).Select(p => p.Text)
        ]);
    }

    public void OnAutoSuggestBoxQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        var message = Messages
            .OfType<TextColloxMessage>()
            .FirstOrDefault(p => p.Text == args.QueryText);
        if (message != null)
        {
            SelectedMessage = message;
        }
    }

    partial void OnSelectedMessageChanged(ColloxMessage value)
    {
        // always scroll to the selected message
        WeakReferenceMessenger.Default.Send(new MessageSelectedMessage(value));
    }

    [RelayCommand]
    public void ChangeModeToCmd()
    {
        SubmitModeIcon = Symbol.Play;
    }

    [RelayCommand]
    public void ChangeModeToWrite()
    {
        SubmitModeIcon = Symbol.Send;
    }

    [RelayCommand]
    public void SwitchMode()
    {
        if (SubmitModeIcon == Symbol.Send)
        {
            ChangeModeToCmd();
        }
        else
        {
            ChangeModeToWrite();
        }
    }

    [RelayCommand]
    public async Task Clear()
    {
        Messages.Clear();
        await storeService.SaveNow();
    }

    [RelayCommand]
    public void SpeakLast()
    {
        if (Messages.Count > 0)
        {
            ReadText(StripMd(Messages.OfType<TextColloxMessage>().Last().Text), SelectedVoice?.Name);
        }
    }

    internal static void ReadText(string text, string voice = null)
    {
        var speechSynthesizer = new SpeechSynthesizer();
        // var voices = speechSynthesizer.GetInstalledVoices();

        speechSynthesizer.SetOutputToDefaultAudioDevice();
        if (voice == null)
        {
            speechSynthesizer.SelectVoiceByHints(VoiceGender.Male, VoiceAge.Adult);
        }
        else
        {
            speechSynthesizer.SelectVoice(voice);
        }

        speechSynthesizer.SpeakAsync(text);
    }

    partial void OnIsBeepingChanged(bool value)
    {
        Settings.AutoBeep = value;
    }

    partial void OnIsSpeakingChanged(bool value)
    {
        Settings.AutoRead = value;
    }

    partial void OnSelectedVoiceChanged(VoiceInfo value)
    {
        Settings.Voice = value.Name;
    }

    [RelayCommand]
    private async Task SaveNow()
    {
        await storeService.SaveNow();
    }

    [RelayCommand]
    private async Task Submit()
    {
        if (string.IsNullOrWhiteSpace(InputMessage))
        {
            return;
        }

        ConversationContext.IsEditing = false;

        switch (SubmitModeIcon)
        {
            case Symbol.Send:
                await AddTextMessage();
                break;
            case Symbol.Play:
                await ProcessCommand();
                break;
        }
    }

    private async Task ProcessCommand()
    {
        var msg = InputMessage;
        InputMessage = string.Empty;
        var tok = msg.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        switch (tok)
        {
            case ["clear", ..]:
                await Clear();
                return;

            case ["save", ..]:
                await SaveNow();
                return;

            case ["speak", ..]:
                SpeakLast();
                return;

            case ["time", ..]:
                var timestampMessage = new TimeColloxMessage
                {
                    Time = DateTime.Now.TimeOfDay
                };

                Messages.Add(timestampMessage);
                return;

            case ["pin", ..]:
                ConversationContext.IsCloseable = false;
                return;


            case ["unpin", ..]:
                ConversationContext.IsCloseable = true;
                return;

            case ["help", ..]:
                var helpMessage = new InternalColloxMessage
                {
                    Message = "Available commands: clear, save, speak, time, pin, unpin, task",
                    Severity = InfoBarSeverity.Informational
                };

                Messages.Add(helpMessage);
                return;
            case ["task", .. var taskName]:
                Tasks.Add(new TaskViewModel { Name = string.Join(" ", taskName), IsDone = false });
                return;
        }
    }

    private async Task AddTextMessage()
    {
        var textMessage = new TextColloxMessage
        {
            Text = InputMessage,
            Timestamp = DateTime.Now,
            IsLoading = true,
            Context = ConversationContext.Context
        };

        Messages.Add(textMessage);

        InputMessage = string.Empty;

        CharacterCount = Math.Min(KeyStrokesCount, CharacterCount + textMessage.Text.Length);

        if (Settings.PersistMessages)
        {
            var singleMessage = new SingleMessage(textMessage.Text, textMessage.Context, textMessage.Timestamp);
            await storeService.Append(singleMessage);
        }

        WeakReferenceMessenger.Default.Send(new TextSubmittedMessage(textMessage));

        if (IsBeeping)
        {
            PlayBeepSound();
        }

        if (IsSpeaking)
        {
            ReadText(textMessage.Text, SelectedVoice?.Name);
        }

        await AddMore(textMessage);
    }

    private static void PlayBeepSound()
    {
        var installedPath = Package.Current.InstalledLocation.Path;
        var sp = new SoundPlayer(Path.Combine(installedPath, "Assets", "notify.wav"));
        sp.Play();
    }

    private async Task AddMore(TextColloxMessage textColloxMessage)
    {
        if (!Settings.EnableAI)
        {
            textColloxMessage.IsLoading = false;
            return;
        }

        try
        {
            var procs = ConversationContext.ActiveProcessors;
            foreach (var processor in procs)
            {
                processor.Process = async (client) => processor.Target switch
                {
                    Target.Comment => await CreateComment(textColloxMessage, processor.Prompt, client),
                    Target.Task => await CreateTask(textColloxMessage, processor.Prompt, client),
                    Target.Context => throw new NotImplementedException(),
                    Target.Chat => await CreateMessage(
                        Messages.OfType<TextColloxMessage>().Where(m => !m.IsGenerated).Select(m => m.Text),
                        processor.Prompt, client),
                    _ => throw new NotImplementedException(),
                };
                processor.OnError = (ex) =>
                {
                    textColloxMessage.ErrorMessage = $"Error: {ex.Message}";
                    textColloxMessage.HasProcessingError = true;
                };
                await processor.Work();
            }
        }
        catch (Exception ex)
        {
            textColloxMessage.ErrorMessage = $"Error: {ex.Message}";
            textColloxMessage.HasProcessingError = true;
        }

        textColloxMessage.IsLoading = false;
    }

    private async Task<string> CreateTask(TextColloxMessage textColloxMessage, string prompt, IChatClient client)
    {
        var response = await client.GetResponseAsync(string.Format(prompt, textColloxMessage.Text));
        Tasks.Add(new TaskViewModel
        {
            Name = response.Text,
            IsDone = false
        });
        return response.Text;
    }

    private async Task<string> CreateMessage(IEnumerable<string> messages, string prompt, IChatClient client)
    {
        var textColloxMessage = new TextColloxMessage
        {
            Text = string.Empty,
            Timestamp = DateTime.Now,
            IsLoading = true,
            IsGenerated = true,
            Context = ConversationContext.Context
        };
        Messages.Add(textColloxMessage);

        var ret = string.Empty;
        var chatMessages = messages.Select(message => new ChatMessage(ChatRole.User, string.Format(prompt, message)));
        await foreach (var update in client.GetStreamingResponseAsync(chatMessages))
        {
            textColloxMessage.Text += update.Text;
            ret += update.Text;
        }

        textColloxMessage.IsLoading = false;
        return ret;
    }

    private static async Task<string> CreateComment(TextColloxMessage textColloxMessage, string prompt,
        IChatClient client)
    {
        var ret = string.Empty;
        await foreach (var update in client.GetStreamingResponseAsync(string.Format(prompt, textColloxMessage.Text)))
        {
            textColloxMessage.Comment += update.Text;
            ret += update.Text;
        }

        return ret;
    }

    private static string StripMd(string mdText)
    {
        var p = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        var content = Markdown.ToPlainText(mdText, p);

        return content;
    }

    public void Receive(TaskDoneMessage message)
    {
        Tasks.Remove(message.Value);
    }
}

public class MessageSelectedMessage(ColloxMessage value) : ValueChangedMessage<ColloxMessage>(value);

public class TextSubmittedMessage(TextColloxMessage value) : ValueChangedMessage<TextColloxMessage>(value);
