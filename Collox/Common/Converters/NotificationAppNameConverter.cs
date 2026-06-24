using Windows.UI.Notifications;
using Microsoft.UI.Xaml.Data;

namespace Collox.Common.Converters;

/// <summary>
///     Safely extracts the source app's display name from a UserNotification.
///     UserNotification.AppInfo throws when the app runs without package identity
///     (unpackaged/self-contained runs), so the getter is guarded.
/// </summary>
public partial class NotificationAppNameConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is not UserNotification notification)
        {
            return string.Empty;
        }

        try
        {
            return notification.AppInfo?.DisplayInfo?.DisplayName ?? string.Empty;
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
