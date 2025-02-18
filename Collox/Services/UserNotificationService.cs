using Windows.UI.Notifications;
using Windows.UI.Notifications.Management;

namespace Collox.Services;

public class UserNotificationService
{
    public delegate void UserNotificationsViewChanged(IReadOnlyList<UserNotification> newView);

    public async Task Initialize()
    {
        // Get the listener
        var listener = UserNotificationListener.Current;

        // And request access to the user's notifications (must be called from UI thread)
        var accessStatus = await listener.RequestAccessAsync();

        switch (accessStatus)
        {
            // This means the user has granted access.
            case UserNotificationListenerAccessStatus.Allowed:
                listener.NotificationChanged += Listener_NotificationChanged;
                // Yay! Proceed as normal
                await UpdateUserNotifications(listener);
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

    private event UserNotificationsViewChanged _userNotificationsViewChanged;

    public event UserNotificationsViewChanged OnUserNotificationsViewChanged
    {
        add => _userNotificationsViewChanged += value;
        //var cnv = UserNotificationListener.Current.GetNotificationsAsync(NotificationKinds.Toast).GetResults();
        //value.Invoke(cnv);
        remove => _userNotificationsViewChanged -= value;
    }

    private void Listener_NotificationChanged(UserNotificationListener sender, UserNotificationChangedEventArgs args)
    {
        UpdateUserNotifications(sender).Wait(TimeSpan.FromSeconds(1));
    }

    private async Task UpdateUserNotifications(UserNotificationListener sender)
    {
        var notifications = await sender.GetNotificationsAsync(NotificationKinds.Toast);
        if (notifications != null)
        {
            _userNotificationsViewChanged?.Invoke(notifications);
        }
    }

    public async Task<IReadOnlyList<UserNotification>> GetNotifications()
    {
        return await UserNotificationListener.Current.GetNotificationsAsync(NotificationKinds.Toast);
    }
}
