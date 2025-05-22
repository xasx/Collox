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

    [ObservableProperty] public partial TimeSpan RelativeTimestamp { get; set; } = TimeSpan.Zero;

    public DateTime Timestamp { get; init; }
}

public partial class TextColloxMessage : ColloxMessage
{
    [ObservableProperty] public partial string Text { get; set; }

    [ObservableProperty] public partial string Comment { get; set; }

    [ObservableProperty] public partial string ErrorMessage { get; set; }

    [ObservableProperty] public partial bool IsLoading { get; set; }

    [ObservableProperty] public partial bool IsGenerated { get; set; } = false;

    [ObservableProperty] public partial Guid GeneratorId { get; set; }

    [ObservableProperty] public partial bool HasProcessingError { get; set; } = false;


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

public partial class  InternalColloxMessage :ColloxMessage
{
    public string  Message { get; set; }
    public InfoBarSeverity Severity { get; set; }
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

