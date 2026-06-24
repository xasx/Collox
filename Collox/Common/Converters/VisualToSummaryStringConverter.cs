using Windows.UI.Notifications;
using Microsoft.UI.Xaml.Data;

namespace Collox.Common.Converters;

public partial class VisualToSummaryStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        // The toast visual is nested under UserNotification.Notification.Visual.
        // Accessing Notification.Visual can throw unpackaged (no package identity),
        // so the whole getter path is guarded. When the visual is unavailable we
        // surface an empty body rather than crashing the binding/render.
        NotificationVisual v;
        try
        {
            v = value switch
            {
                UserNotification un => un?.Notification?.Visual,
                NotificationVisual visual => visual,
                _ => null
            };
        }
        catch
        {
            return string.Empty;
        }

        if (v is null)
        {
            return string.Empty;
        }

        // Get the toast binding, if present
        var toastBinding = v.GetBinding(KnownNotificationBindings.ToastGeneric);

        if (toastBinding != null)
        {
            // And then get the text elements from the toast binding
            var textElements = toastBinding.GetTextElements();

            // Treat the first text element as the title text
            var titleText = textElements.Count > 0 ? textElements[0].Text : null;

            // We'll treat all subsequent text elements as body text,
            // joining them together via newlines.
            var bodyText = string.Join("\n", textElements.Skip(1).Select(t => t.Text));

            return $"{titleText}\n{bodyText}";
        }

        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
