using System.Collections.ObjectModel;
using System.Speech.Synthesis;
using System.Timers;
using Collox.Services;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Windows.System;

namespace Collox.ViewModels;

public partial class Paragraph : ObservableObject
{
    internal static DispatcherQueue DispatcherQueue = DispatcherQueue.GetForCurrentThread();

    private static readonly System.Timers.Timer Timer = new System.Timers.Timer()
    {
        Interval = 10000,
        Enabled = true
    };
    public Paragraph()
    {
        var wel = new WeakEventListener<Paragraph, object, ElapsedEventArgs>(this)
        {
            OnEventAction = (instance, sender, args) =>
            {
                Paragraph.DispatcherQueue.TryEnqueue(() =>
                {
                    instance.RelativeTimestamp = DateTime.Now - instance.Timestamp;
                });
            },
            OnDetachAction = (listener) => Timer.Elapsed -= listener.OnEvent
        };

        Timer.Elapsed += wel.OnEvent;
    }

    [ObservableProperty]
    public partial int AdditionalSpacing { get; set; } = 0;

    [ObservableProperty]
    public partial TimeSpan RelativeTimestamp { get; set; }

    public string Text { get; set; }

    public DateTime Timestamp { get; set; }
    [RelayCommand]
    public async Task Read()
    {
        WriteViewModel.ReadText(Text, AppHelper.Settings.Voice);
    }
}

public partial class WriteViewModel : ObservableObject, ITitleBarAutoSuggestBoxAware
{
    private static ICollection<VoiceInfo> voiceInfos = new SpeechSynthesizer().GetInstalledVoices().Select(iv => iv.VoiceInfo).ToList();
    private IStoreService storeService;

    public WriteViewModel()
    {
        storeService = App.GetService<IStoreService>();
    }

    [ObservableProperty]
    public partial int CharacterCount { get; set; }

    public ICollection<VoiceInfo> InstalledVoices
    {
        get
        {
            return voiceInfos;
        }
    }

    [ObservableProperty]
    public partial bool IsBeeping { get; set; } = AppHelper.Settings.AutoBeep;

    [ObservableProperty]
    public partial bool IsSpeaking { get; set; } = AppHelper.Settings.AutoRead;

    [ObservableProperty]
    public partial int KeyStrokesCount { get; set; }

    [ObservableProperty]
    public partial string LastParagraph { get; set; } = string.Empty;

    [ObservableProperty]
    public partial ObservableCollection<Paragraph> Paragraphs { get; set; } = [];

    [ObservableProperty]
    public partial VoiceInfo SelectedVoice { get; set; }
        = voiceInfos.Where(vi => vi.Name == AppHelper.Settings.Voice).FirstOrDefault();

    [ObservableProperty]
    public partial Symbol SubmitModeIcon { get; set; } = Symbol.Send;

    [RelayCommand]
    public async Task ChangeModeToCmd()
    {
        SubmitModeIcon = Symbol.Play;
    }

    [RelayCommand]
    public async Task ChangeModeToWrite()
    {
        SubmitModeIcon = Symbol.Send;
    }

    [RelayCommand]
    public async Task Clear()
    {
        Paragraphs.Clear();
        await storeService.SaveNow();
    }

    [RelayCommand]
    public async Task SpeakLast()
    {
        if (Paragraphs.Count > 0)
        {
            ReadText(Paragraphs.Last().Text, SelectedVoice?.Name);
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
        AppHelper.Settings.AutoRead = value;
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


        LastParagraph = string.Empty;
    }

    private async Task ProcessCommand()
    {

        switch (LastParagraph)
        {
            case "clear":
                await Clear();
                return;

            case "save":
                await SaveNow();
                return;

            case "speak":
                await SpeakLast();
                return;

            case "..":
                Paragraphs.Last().AdditionalSpacing += 42;
                return;
        }

    }

    private async Task AddParagraph()
    {
        var paragraph = new Paragraph()
        {
            Text = LastParagraph,
            Timestamp = DateTime.Now
        };
        Paragraphs.Add(paragraph);

        CharacterCount = Math.Min(KeyStrokesCount, CharacterCount + LastParagraph.Length);
        await storeService.AppendParagraph(paragraph.Text, paragraph.Timestamp);
        WeakReferenceMessenger.Default.Send(new TextSubmittedMessage(LastParagraph));

        if (IsBeeping)
        {
            var installedPath = Windows.ApplicationModel.Package.Current.InstalledLocation.Path;
            var sp = new System.Media.SoundPlayer(Path.Combine(installedPath, "Assets", "noti.wav"));
            sp.Play();
        }

        if (IsSpeaking)
        {
            ReadText(LastParagraph, SelectedVoice?.Name);
        }
    }

    public void OnAutoSuggestBoxTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        DevWinUI .AutoSuggestBoxHelper.LoadSuggestions(sender, args, Paragraphs.Where(p => p.Text.Contains( sender.Text)).Select(p => p.Text).ToArray());

    }

    public void OnAutoSuggestBoxQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        // args.QueryText
        // find the paragraph with the text and determine the index
        // then scroll to the index
        var paragraph = Paragraphs.FirstOrDefault(p => p.Text == args.QueryText);
        if (paragraph != null)
        {
            var index = Paragraphs.IndexOf(paragraph);
            WeakReferenceMessenger.Default.Send(new ParagraphSelectedMessage(index));
        }

    }
}

public class ParagraphSelectedMessage : ValueChangedMessage<int>
{   
    public ParagraphSelectedMessage(int index) : base(index)
    {
    }
}

public class TextSubmittedMessage : ValueChangedMessage<string>
{
    public TextSubmittedMessage(string value) : base(value)
    {
    }
}
