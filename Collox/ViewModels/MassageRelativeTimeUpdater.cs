using Windows.System;

namespace Collox.ViewModels;

public sealed class MassageRelativeTimeUpdater
{
    public static Func<DispatcherQueueTimer> CreateTimer { get; set; }

    private readonly DispatcherQueueTimer Timer = CreateTimer();

    public MassageRelativeTimeUpdater()
    {
        Timer.Interval = TimeSpan.FromMilliseconds(3333);
        Timer.IsRepeating = true;
        Timer.Start();
    }

    public void RegisterMessage(ColloxMessage colloxMessage)
    {
        ColloxWeakEventListener colloxWeakEventListener = new(colloxMessage) { Timer = Timer };
        Timer.Tick += colloxWeakEventListener.OnTimerTick;
    }
}
internal class ColloxWeakEventListener(ColloxMessage colloxMessage)
{
    private readonly WeakReference<ColloxMessage> _weakInstance = new(colloxMessage);

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
