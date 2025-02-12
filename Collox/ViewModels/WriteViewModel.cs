using System.Collections.ObjectModel;
using System.Speech.Synthesis;
using Collox.Services;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Markdig;
using Microsoft.Extensions.AI;

namespace Collox.ViewModels;

public partial class WriteViewModel : ObservableObject, ITitleBarAutoSuggestBoxAware
{
    private static readonly ICollection<VoiceInfo> voiceInfos =
        [.. new SpeechSynthesizer().GetInstalledVoices().Select(iv => iv.VoiceInfo)];

    private readonly IStoreService storeService = App.GetService<IStoreService>();

    [ObservableProperty] public partial int CharacterCount { get; set; }

    [ObservableProperty]
    public partial TabData ConversationContext { get; set; }

    public ICollection<VoiceInfo> InstalledVoices
    {
        get
        {
            return voiceInfos;
        }
    }

    [ObservableProperty] public partial bool IsBeeping { get; set; } = AppHelper.Settings.AutoBeep;

    [ObservableProperty] public partial bool IsSpeaking { get; set; } = AppHelper.Settings.AutoRead;

    [ObservableProperty] public partial int KeyStrokesCount { get; set; }

    [ObservableProperty] public partial string LastParagraph { get; set; } = string.Empty;

    [ObservableProperty] public partial ObservableCollection<Paragraph> Paragraphs { get; set; } = [];

    [ObservableProperty]
    public partial VoiceInfo SelectedVoice { get; set; }
        = voiceInfos.FirstOrDefault(vi => vi.Name == Settings.Voice);

    [ObservableProperty] public partial Symbol SubmitModeIcon { get; set; } = Symbol.Send;

    [ObservableProperty] public partial string Filename { get; set; }

    [ObservableProperty] public partial bool ClockShown { get; set; }

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
        Paragraphs.Clear();
        await storeService.SaveNow();
    }

    [RelayCommand]
    public void SpeakLast()
    {
        if (Paragraphs.Count > 0)
        {
            ReadText(StripMd(Paragraphs.Where(p => p is TextParagraph)
                .Cast<TextParagraph>().Last().Text), SelectedVoice?.Name);
        }
    }

    internal static void ReadText(string text, string voice = null)
    {
        var speechSynthesizer = new SpeechSynthesizer();
        // var voices = speechSynthesizer.GetInstalledVoices();

        speechSynthesizer.SetOutputToDefaultAudioDevice();
        if (voice == null)
            speechSynthesizer.SelectVoiceByHints(VoiceGender.Male, VoiceAge.Adult);
        else
            speechSynthesizer.SelectVoice(voice);

        speechSynthesizer.SpeakAsync(text);
    }

    partial void OnIsBeepingChanged(bool value)
    {
        AppHelper.Settings.AutoBeep = value;
    }

    partial void OnIsSpeakingChanged(bool value)
    {
        AppHelper.Settings.AutoRead = value;
    }

    partial void OnSelectedVoiceChanged(VoiceInfo value)
    {
        AppHelper.Settings.Voice = value.Name;
    }

    [RelayCommand]
    private async Task SaveNow()
    {
        await storeService.SaveNow();
    }

    [RelayCommand]
    private async Task Submit()
    {
        if (string.IsNullOrWhiteSpace(LastParagraph))
        {
            return;
        }

        switch (SubmitModeIcon)
        {
            case Symbol.Send:
                await AddParagraph();
                break;
            case Symbol.Play:
                await ProcessCommand();
                break;
        }
        // Any code here will run after processing the command or adding the paragraph
    }

    private async Task ProcessCommand()
    {
        LastParagraph = string.Empty;

        switch (LastParagraph)
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
                Paragraphs.Last().AdditionalSpacing += 42;
                return;

            case "time":
                var timeParagraph = new TimeParagraph()
                {
                    Time = DateTime.Now.TimeOfDay,
                };

                Paragraphs.Add(timeParagraph);
                return;
        }
    }

    private async Task AddParagraph()
    {
        var paragraph = new TextParagraph()
        {
            Text = LastParagraph,
            Timestamp = DateTime.Now,
            IsLoading = true
        };
        Paragraphs.Add(paragraph);

        LastParagraph = string.Empty;

        CharacterCount = Math.Min(KeyStrokesCount, CharacterCount + paragraph.Text.Length);
        await storeService.AppendParagraph(paragraph.Text, ConversationContext.Context, paragraph.Timestamp);
        WeakReferenceMessenger.Default.Send(new TextSubmittedMessage(paragraph.Text));

        if (IsBeeping)
        {
            var installedPath = Windows.ApplicationModel.Package.Current.InstalledLocation.Path;
            var sp = new System.Media.SoundPlayer(Path.Combine(installedPath, "Assets", "noti.wav"));
            sp.Play();
        }

        if (IsSpeaking)
        {
            ReadText(paragraph.Text, SelectedVoice?.Name);
        }

        await AddComment(paragraph);
    }

    private async Task AddComment(TextParagraph textParagraph)
    {
        try
        {
            IChatClient client = new OllamaChatClient(
                new Uri("http://localhost:11434/"), "phi4");

            var prompt = $"""
                 What is the essence of the following text? Please stick to the language of the text and limit your answer to a few words.
                 
                 {textParagraph.Text}
                 
                 """;

            await foreach (var update in client.CompleteStreamingAsync(prompt))
            {
                textParagraph.Comment += update.Text;
            }
        }
        catch (Exception)
        {
            textParagraph.Comment = "Error";
        }

        textParagraph.IsLoading = false;
    }

    private static string StripMd(string mdText)
    {
        var p = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        var content = Markdown.ToPlainText(mdText, p);

        return content;
    }

    public void OnAutoSuggestBoxTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        AutoSuggestBoxHelper.LoadSuggestions(sender, args, [.. Paragraphs
            .Where(p => p is TextParagraph).Cast<TextParagraph>()
            .Where(p => p.Text.Contains(sender.Text)).Select(p => p.Text)]);
    }

    public void OnAutoSuggestBoxQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        // args.QueryText
        // find the paragraph with the text and determine the index
        // then scroll to the index
        var paragraph = Paragraphs
            .Where(p => p is TextParagraph).Cast<TextParagraph>()
            .FirstOrDefault(p => p.Text == args.QueryText);
        if (paragraph != null)
        {
            var index = Paragraphs.IndexOf(paragraph);
            WeakReferenceMessenger.Default.Send(new ParagraphSelectedMessage(index));
        }
    }
}

public class ParagraphSelectedMessage(int index) : ValueChangedMessage<int>(index);

public class TextSubmittedMessage(string value) : ValueChangedMessage<string>(value);
