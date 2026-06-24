using Windows.UI.Notifications;
using Microsoft.UI.Xaml.Data;

namespace Collox.Common.Converters;

/// <summary>
///     Safely extracts the creation time from a UserNotification.
///     Although CreationTime is typically safe, it is accessed from the same
///     render path as AppInfo/Notification, which can throw unpackaged, so the
///     getter is guarded for consistency.
/// </summary>
public partial class NotificationTimeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is not UserNotification notification)
        {
            return string.Empty;
        }

        try
        {
            return notification.CreationTime.ToString("g");
        }
        catch
        {
            return string.Empty;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
