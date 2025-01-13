using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Collox.Services;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using System.Speech.Synthesis;
using System.Globalization;
using ABI.Windows.Storage.Streams;
using System.Collections;
using Windows.Win32;
using Windows.Win32.Foundation;
using System.Timers;
using Windows.System;

namespace Collox.ViewModels;

public partial class WriteViewModel : ObservableObject
{
    private IStoreService storeService;

    public WriteViewModel()
    {
        storeService = App.GetService<IStoreService>();
    }

    [ObservableProperty]
    public partial string LastParagraph { get; set; } = string.Empty;

    [ObservableProperty]
    public partial ObservableCollection<Paragraph> Paragraphs { get; set; } = [];

    [ObservableProperty]
    public partial int CharacterCount { get; set; }

    [ObservableProperty]
    public partial int KeyStrokesCount { get; set; }

    [ObservableProperty]
    public partial bool IsSpeaking { get; set; } = AppHelper.Settings.AutoRead;

    [ObservableProperty]
    public partial bool IsBeeping { get; set; } = AppHelper.Settings.AutoBeep;

    [ObservableProperty]
    public partial Symbol SubmitModeIcon { get; set; } = Symbol.Send;

    [RelayCommand]
    private async Task Submit()
    {
        if (string.IsNullOrWhiteSpace(LastParagraph))
        {
            return;
        }
        if (LastParagraph.StartsWith("."))
        {
            switch (LastParagraph)
            {
                case ".clear":
                    await Clear();
                    return;

                case ".save":
                    await SaveNow();
                    return;

                case ".speak":
                    await SpeakLast();
                    return;

                case "..":
                    Paragraphs.Last().AdditionalSpacing += 42;
                    return;
            }
        }

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
        LastParagraph = string.Empty;
    }

    private static ICollection<VoiceInfo> voiceInfos = new SpeechSynthesizer().GetInstalledVoices().Select(iv => iv.VoiceInfo).ToList();

    public ICollection<VoiceInfo> InstalledVoices
    {
        get
        {
            return voiceInfos;
        }
    }

    [ObservableProperty]
    public partial VoiceInfo SelectedVoice { get; set; }
        = voiceInfos.Where(vi => vi.Name == AppHelper.Settings.Voice).FirstOrDefault();

    partial void OnSelectedVoiceChanged(VoiceInfo value)
    {
        AppHelper.Settings.Voice = value.Name;
    }

    partial void OnIsBeepingChanged(bool value)
    {
        AppHelper.Settings.AutoRead = value;
    }

    partial void OnIsSpeakingChanged(bool value)
    {
        AppHelper.Settings.AutoRead = value;
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

    [RelayCommand]
    private async Task SaveNow()
    {
        await storeService.SaveNow();
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
}

public partial class Paragraph : ObservableObject
{
    private static readonly System.Timers.Timer Timer = new System.Timers.Timer()
    {
        Interval = 10000,
        Enabled = true
    };

    private static readonly DispatcherQueue DispatcherQueue = DispatcherQueue.GetForCurrentThread();

    public Paragraph()
    {
        Timer.Elapsed += (sender, e) =>
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                this.RelativeTimestamp = DateTime.Now - this.Timestamp;
            });
        };
    }

    [ObservableProperty]
    public partial TimeSpan RelativeTimestamp { get; set; }

    public string Text { get; set; }

    public DateTime Timestamp { get; set; }

    [ObservableProperty]
    public partial int AdditionalSpacing { get; set; } = 0;

    [RelayCommand]
    public async Task Read()
    {
        WriteViewModel.ReadText(Text, AppHelper.Settings.Voice);
    }
}

public class TextSubmittedMessage : ValueChangedMessage<string>
{
    public TextSubmittedMessage(string value) : base(value)
    {
    }
}
