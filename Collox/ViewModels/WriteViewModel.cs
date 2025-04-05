using System.ClientModel;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Media;
using System.Speech.Synthesis;
using Collox.Models;
using Collox.Services;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Markdig;
using Microsoft.Extensions.AI;
using OpenAI.Chat;
using Windows.ApplicationModel;

namespace Collox.ViewModels;

public partial class WriteViewModel : ObservableObject, ITitleBarAutoSuggestBoxAware
{
    private static readonly ICollection<VoiceInfo> voiceInfos =
        [.. new SpeechSynthesizer().GetInstalledVoices().Select(iv => iv.VoiceInfo)];

    private readonly IStoreService storeService = App.GetService<IStoreService>();

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

        switch (msg)
        {
            case "clear":
                await Clear();
                return;

            case "save":
                await SaveNow();
                return;

            case "speak":
                SpeakLast();
                return;

            case "..":
                Messages.Last().AdditionalSpacing += 42;
                return;

            case "time":
                var timestampMessage = new TimeColloxMessage
                {
                    Time = DateTime.Now.TimeOfDay
                };

                Messages.Add(timestampMessage);
                return;

            case "pin":
                ConversationContext.IsCloseable = false;
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

        if (AppHelper.Settings.PersistMessages)
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

        await AddComment(textMessage);
    }

    private static void PlayBeepSound()
    {
        var installedPath = Package.Current.InstalledLocation.Path;
        var sp = new SoundPlayer(Path.Combine(installedPath, "Assets", "notify.wav"));
        sp.Play();
    }

    private async Task AddComment(TextColloxMessage textColloxMessage)
    {
        if (!Settings.EnableAI)
        {
            textColloxMessage.IsLoading = false;
            return;
        }

        try
        {
            var client = //new OllamaChatClient(new Uri("http://localhost:11434/"), "phi4");
                new ChatClient(AppHelper.Settings.OpenAIModelId,
                new ApiKeyCredential(AppHelper.Settings.OpenAIApiKey),
                new OpenAI.OpenAIClientOptions()
                {
                    Endpoint = new Uri(AppHelper.Settings.OpenAIEndpoint)
                }).AsChatClient();

            var prompt = $"""
                          Please give me a couple of alternatives to the following text between BEGIN and END
                          Stick to the language of the sentence. Only output the alternatives.

                          BEGIN
                          {textColloxMessage.Text}
                          END
                          """;

            await foreach (var update in client.GetStreamingResponseAsync(prompt))
            {
                Debug.WriteLine(update.Text);
                textColloxMessage.Comment += update.Text;
            }
        }
        catch (Exception ex)
        {
            textColloxMessage.Comment = $"Error: {ex.Message}";
        }

        textColloxMessage.IsLoading = false;
    }

    private static string StripMd(string mdText)
    {
        var p = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        var content = Markdown.ToPlainText(mdText, p);

        return content;
    }
}

public class MessageSelectedMessage(ColloxMessage value) : ValueChangedMessage<ColloxMessage>(value);

public class TextSubmittedMessage(TextColloxMessage value) : ValueChangedMessage<TextColloxMessage>(value);
