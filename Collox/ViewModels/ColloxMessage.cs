using System.Timers;
using Windows.System;
using Timer = System.Timers.Timer;

namespace Collox.ViewModels;

public partial class ColloxMessage : ObservableObject
{
    private static readonly DispatcherQueue DispatcherQueue = DispatcherQueue.GetForCurrentThread();

    private static readonly DispatcherQueueTimer Timer = DispatcherQueue.CreateTimer();

    static ColloxMessage() {
        Timer.Interval = TimeSpan.FromMilliseconds(3333);
        Timer.IsRepeating = true;
        Timer.Start();
    }

    protected ColloxMessage()
    {
        ColloxWeakEventListener colloxWeakEventListener = new(this)
        {
            Timer = ColloxMessage.Timer
        };
        Timer.Tick += colloxWeakEventListener.OnTimerTick;
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

    public string Context { get; internal init; }

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

internal class ColloxWeakEventListener
{
    private readonly WeakReference<ColloxMessage> _weakInstance;

    public ColloxWeakEventListener(ColloxMessage colloxMessage)
    {
        _weakInstance = new WeakReference<ColloxMessage>(colloxMessage);
    }

    public DispatcherQueueTimer Timer { get; init; }

    public void OnTimerTick(DispatcherQueueTimer sender, object args)
    {
        if (_weakInstance.TryGetTarget(out var target))
        {
            target.RelativeTimestamp = DateTime.Now - target.Timestamp;
        }
        else
        {
            Timer.Tick -= OnTimerTick;
        }
    }
}

