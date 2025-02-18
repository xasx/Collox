using Windows.UI.Notifications;
using Microsoft.UI.Xaml.Data;

namespace Collox.Common.Converters;

public partial class VisualToSummaryStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var v = value as NotificationVisual;

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
