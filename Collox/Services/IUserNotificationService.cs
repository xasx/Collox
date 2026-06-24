using Windows.UI.Notifications;

namespace Collox.Services;

public interface IUserNotificationService
{
    public delegate void UserNotificationsViewChanged(IReadOnlyList<UserNotification> newView);

    event UserNotificationsViewChanged OnUserNotificationsViewChanged;

    Task<IReadOnlyList<UserNotification>> GetNotifications();

    Task Initialize();

    void DismissNotification(uint notificationId);

    void DismissAllNotifications();
}
