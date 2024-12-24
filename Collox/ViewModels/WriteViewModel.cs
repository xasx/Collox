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
    public partial int CharacterCount { get; set;  }

    [ObservableProperty]
    public partial int KeyStrokesCount { get; set; }

    [ObservableProperty]
    public partial bool IsSpeaking { get; set; } = false;

    [ObservableProperty]
    public partial bool IsBeeping { get; set; } = false;

    [RelayCommand]
    private async Task Submit()
    {
        var paragraph = new Paragraph()
        {
            Text = LastParagraph,
            Timestamp = DateTime.Now
        };
        Paragraphs.Add(paragraph);
        CharacterCount += LastParagraph.Length;
        await storeService.AppendParagraph(paragraph.Text, paragraph.Timestamp);
        WeakReferenceMessenger.Default.Send(new TextSubmittedMessage(LastParagraph));
        if (IsBeeping)
        {
            
            
            var installedPath = Windows.ApplicationModel.Package.Current.InstalledLocation.Path;
           var sp = new System.Media.SoundPlayer(Path.Combine(installedPath,"Assets","noti.wav"));
            sp.Play();
        }
        if (IsSpeaking)
        {
            ReadText(LastParagraph);
        }
        LastParagraph = string.Empty;

    }

    internal static void ReadText(string text)
    {
        var speechSynthesizer = new SpeechSynthesizer();
        // var voices = speechSynthesizer.GetInstalledVoices();

        speechSynthesizer.SetOutputToDefaultAudioDevice();
        speechSynthesizer.SelectVoiceByHints(VoiceGender.Male, VoiceAge.Adult);
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

}

public partial class Paragraph
{
    public string Text { get; set; }

    public DateTime Timestamp { get; set; }

    [RelayCommand]
    public async Task Read() {
        WriteViewModel.ReadText(Text);
    }
}


public class TextSubmittedMessage : ValueChangedMessage<string>
{
    public TextSubmittedMessage(string value) : base(value)
    {
    }
}
