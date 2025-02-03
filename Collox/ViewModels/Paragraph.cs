using System.Timers;
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

    public DateTime Timestamp { get; set; }
    
}

public partial class TextParagraph : Paragraph
{

    public string Text { get; set; }

    [RelayCommand]
    public async Task Read()
    {
        WriteViewModel.ReadText(Text, AppHelper.Settings.Voice);
    }
}

public partial class  TimeParagraph : Paragraph
{
    public TimeSpan Time { get; set; }
}

