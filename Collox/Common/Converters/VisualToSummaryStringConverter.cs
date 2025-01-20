using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Data;
using Windows.UI.Notifications;

namespace Collox.Common.Converters;

class VisualToSummaryStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {

        var v = value as NotificationVisual;


        // Get the toast binding, if present
        NotificationBinding toastBinding = v.GetBinding(KnownNotificationBindings.ToastGeneric);

        if (toastBinding != null)
        {
            // And then get the text elements from the toast binding
            IReadOnlyList<AdaptiveNotificationText> textElements = toastBinding.GetTextElements();

            // Treat the first text element as the title text
            string titleText = textElements.FirstOrDefault()?.Text;

            // We'll treat all subsequent text elements as body text,
            // joining them together via newlines.
            string bodyText = string.Join("\n", textElements.Skip(1).Select(t => t.Text));

            return $"{titleText}\n{bodyText}";
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
