namespace Collox.ViewModels;

public sealed class MessageRelativeTimeUpdater
{
    public static Func<ITimer> CreateTimer { get; set; }

    private readonly ITimer Timer = CreateTimer();

    public MessageRelativeTimeUpdater()
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

    public ITimer Timer { get; init; }

    public void OnTimerTick(ITimer sender, object args)
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
