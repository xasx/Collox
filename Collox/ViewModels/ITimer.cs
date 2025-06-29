using Windows.Foundation;
using Windows.System;

namespace Collox.ViewModels;

public interface ITimer
{

    event TypedEventHandler<ITimer, object> Tick;

    void Start();

    void Stop();

    TimeSpan Interval { get; set; }

    bool IsRepeating { get; set; }
}

// Fix for CS0029 and CS1662: Implement an adapter to convert DispatcherQueueTimer to ITimer
public class DispatcherQueueTimerAdapter : ITimer
{
    private readonly DispatcherQueueTimer _timer;

    public DispatcherQueueTimerAdapter(DispatcherQueueTimer timer) { _timer = timer; }

    public event TypedEventHandler<ITimer, object> Tick
    {
        add => _timer.Tick += (sender, args) => value(this, args);
        remove => _timer.Tick -= (sender, args) => value(this, args);
    }

    public void Start() => _timer.Start();

    public void Stop() => _timer.Stop();

    public TimeSpan Interval { get => _timer.Interval; set => _timer.Interval = value; }

    public bool IsRepeating { get => _timer.IsRepeating; set => _timer.IsRepeating = value; }
}
