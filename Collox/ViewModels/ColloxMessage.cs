using System.Diagnostics;
using System.Timers;
using Windows.System;
using Timer = System.Timers.Timer;

namespace Collox.ViewModels;

public partial class ColloxMessage : ObservableObject
{
    private static readonly DispatcherQueue DispatcherQueue = DispatcherQueue.GetForCurrentThread();

    private static readonly Timer Timer = new()
    {
        Interval = 3333,
        Enabled = true
    };

    protected ColloxMessage()
    {
        var wel = new WeakEventListener<ColloxMessage, object, ElapsedEventArgs>(this)
        {
            OnEventAction = (instance, sender, args) => DispatcherQueue
                .TryEnqueue(() => instance.RelativeTimestamp = DateTime.Now - instance.Timestamp),
            OnDetachAction = listener => Timer.Elapsed -= listener.OnEvent
        };

        Timer.Elapsed += wel.OnEvent;
    }

    [ObservableProperty] public partial int AdditionalSpacing { get; set; } = 0;

    [ObservableProperty] public partial TimeSpan RelativeTimestamp { get; set; } = TimeSpan.Zero;

    public DateTime Timestamp { get; init; }
}

public partial class TextColloxMessage : ColloxMessage
{
    public string Text { get; init; }

    [ObservableProperty] public partial string Comment { get; set; }

    [ObservableProperty] public partial bool IsLoading { get; set; }

    [RelayCommand]
    public void Read()
    {
        WriteViewModel.ReadText(Text, Settings.Voice);
    }
}

public partial class TimeColloxMessage : ColloxMessage
{
    public TimeSpan Time { get; init; }
}
