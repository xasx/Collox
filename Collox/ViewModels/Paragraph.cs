using System.Timers;
using Windows.System;

namespace Collox.ViewModels;

public partial class Paragraph : ObservableObject
{
    private static readonly DispatcherQueue DispatcherQueue = DispatcherQueue.GetForCurrentThread();

    private static readonly System.Timers.Timer Timer = new()
    {
        Interval = 10000,
        Enabled = true
    };

    protected Paragraph()
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

    public DateTime Timestamp { get; init; }

}

public partial class TextParagraph : Paragraph
{

    public string Text { get; init; }

    [ObservableProperty]
    public partial string Comment { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [RelayCommand]
    public void Read()
    {
        WriteViewModel.ReadText(Text, AppHelper.Settings.Voice);
    }
}

public partial class  TimeParagraph : Paragraph
{
    public TimeSpan Time { get; init; }
}

