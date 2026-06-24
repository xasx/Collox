using Serilog;
using Windows.UI.Notifications;
using Windows.UI.Notifications.Management;
using static Collox.Services.IUserNotificationService;

namespace Collox.Services;

public class UserNotificationService : IUserNotificationService, IDisposable
{
    // 0x80070490 (ERROR_NOT_FOUND): subscribing to NotificationChanged throws this
    // when the app runs unpackaged/self-contained. Tracked in microsoft/WindowsAppSDK #6172.
    private const int ErrorNotFound = unchecked((int)0x80070490);

    // Only show notifications from the last 24 hours. GetNotificationsAsync returns
    // the full Windows notification history, which often includes stale/dismissed
    // notifications from days or weeks ago.
    private static readonly TimeSpan RecentWindow = TimeSpan.FromHours(24);

    private static readonly ILogger Logger = Log.ForContext<UserNotificationService>();

    /// <summary>
    ///     Filters notifications to only those within the recent window that are
    ///     still backed by a valid app (AppInfo accessible) and have not been
    ///     explicitly dismissed by the user.
    /// </summary>
    private IReadOnlyList<UserNotification> FilterRecent(IReadOnlyList<UserNotification> notifications)
    {
        var cutoff = DateTimeOffset.Now - RecentWindow;
        return notifications
            .Where(n => n.CreationTime >= cutoff)
            .Where(n => !_dismissedIds.Contains(n.Id))
            .Where(IsAppInfoAccessible)
            .ToArray();
    }

    private static bool IsAppInfoAccessible(UserNotification n)
    {
        // Notifications from uninstalled or disabled apps throw on AppInfo access.
        // These are ghost entries in wpndatabase.db that no longer appear in Action Center.
        try
        {
            var _ = n.AppInfo?.DisplayInfo?.DisplayName;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private readonly HashSet<uint> _dismissedIds = [];
    private IReadOnlyList<UserNotification> _lastRawNotifications = [];

    public void DismissNotification(uint notificationId)
    {
        _dismissedIds.Add(notificationId);
    }

    public void DismissAllNotifications()
    {
        // Record all current notification IDs as dismissed so they stay
        // filtered out across poll/event refreshes.
        foreach (var n in _lastRawNotifications)
        {
            _dismissedIds.Add(n.Id);
        }
        _userNotificationsViewChanged?.Invoke([]);
    }

    private UserNotificationListener _listener;
    private bool _disposed;
    private bool _eventSubscribed;
    private PeriodicTimer _pollingTimer;
    private CancellationTokenSource _pollingCts;

    public async Task Initialize()
    {
        // Get the listener
        _listener = UserNotificationListener.Current;

        // And request access to the user's notifications (must be called from UI thread)
        var accessStatus = await _listener.RequestAccessAsync();

        switch (accessStatus)
        {
            // This means the user has granted access.
            case UserNotificationListenerAccessStatus.Allowed:
                SubscribeToNotificationChanged();
                await UpdateUserNotifications(_listener).ConfigureAwait(false);
                break;

            // This means the user has denied access.
            // Any further calls to RequestAccessAsync will instantly
            // return Denied. The user must go to the Windows settings
            // and manually allow access.
            case UserNotificationListenerAccessStatus.Denied:

                // Show UI explaining that listener features will not
                // work until user allows access.
                break;

            // This means the user closed the prompt without
            // selecting either allow or deny. Further calls to
            // RequestAccessAsync will show the dialog again.
            case UserNotificationListenerAccessStatus.Unspecified:

                // Show UI that allows the user to bring up the prompt again
                break;
        }
    }

    /// <summary>
    ///     Subscribes to the listener's change event. Falls back to polling
    ///     when the event subscription is unavailable (see ErrorNotFound / WindowsAppSDK #6172).
    /// </summary>
    private void SubscribeToNotificationChanged()
    {
        try
        {
            _listener.NotificationChanged += Listener_NotificationChanged;
            _eventSubscribed = true;
        }
        catch (Exception ex) when (ex.HResult == ErrorNotFound)
        {
            // Known issue: NotificationChanged throws ERROR_NOT_FOUND when the
            // app runs without package identity (unpackaged/self-contained dev
            // runs). GetNotificationsAsync still works, so poll instead.
            Logger.Warning(ex,
                "UserNotificationListener.NotificationChanged unavailable; falling back to polling");
            StartPollingFallback();
        }
    }

    /// <summary>
    ///     Polls GetNotificationsAsync periodically to keep the view fresh when
    ///     the NotificationChanged event cannot be subscribed to.
    /// </summary>
    private void StartPollingFallback()
    {
        _pollingCts = new CancellationTokenSource();
        _pollingTimer = new PeriodicTimer(TimeSpan.FromSeconds(5));

        _ = Task.Run(async () =>
        {
            try
            {
                while (await _pollingTimer.WaitForNextTickAsync(_pollingCts.Token).ConfigureAwait(false))
                {
                    await UpdateUserNotifications(_listener).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected on shutdown.
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Notification polling fallback failed");
            }
        }, _pollingCts.Token);
    }

    private event UserNotificationsViewChanged _userNotificationsViewChanged;

    public event UserNotificationsViewChanged OnUserNotificationsViewChanged
    {
        add => _userNotificationsViewChanged += value;
        //var cnv = UserNotificationListener.Current.GetNotificationsAsync(NotificationKinds.Toast).GetResults();
        //value.Invoke(cnv);
        remove => _userNotificationsViewChanged -= value;
    }

    private void Listener_NotificationChanged(UserNotificationListener sender, UserNotificationChangedEventArgs args)
    { UpdateUserNotifications(sender).Wait(TimeSpan.FromSeconds(1)); }

    private async Task UpdateUserNotifications(UserNotificationListener sender)
    {
        var notifications = await sender.GetNotificationsAsync(NotificationKinds.Toast);
        if (notifications != null)
        {
            _lastRawNotifications = notifications;
            _userNotificationsViewChanged?.Invoke(FilterRecent(notifications));
        }
    }

    public async Task<IReadOnlyList<UserNotification>> GetNotifications()
    {
        var notifications = await UserNotificationListener.Current.GetNotificationsAsync(NotificationKinds.Toast);
        if (notifications is not null)
        {
            _lastRawNotifications = notifications;
            return FilterRecent(notifications);
        }
        return [];
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        if (_listener != null && _eventSubscribed)
        {
            _listener.NotificationChanged -= Listener_NotificationChanged;
        }

        _pollingCts?.Cancel();
        _pollingCts?.Dispose();
        _pollingTimer?.Dispose();

        _disposed = true;
    }
}
