using Microsoft.UI.Xaml.Data;
using Microsoft.Windows.ApplicationModel.Resources;

namespace Collox.Common.Converters;

public partial class TimeSpanToFriendlyConverter : IValueConverter
{
    public string SecondsAgo { get; set; } = "{0} seconds ago";
    public string MinutesAgo { get; set; } = "{0} minutes ago";
    public string HoursAgo { get; set; } = "{0} hours ago";
    public string DaysAgo { get; set; } = "{0} days ago";
    public string WeeksAgo { get; set; } = "{0} weeks ago";
    public string MonthsAgo { get; set; } = "{0} months ago";
    public string YearsAgo { get; set; } = "{0} years ago";

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var timeSpan = (TimeSpan)value;
        if (timeSpan.TotalSeconds < 60)
        {
            return string.Format(SecondsAgo, timeSpan.Seconds);
        }

        if (timeSpan.TotalMinutes < 60)
        {
            return string.Format(MinutesAgo, timeSpan.Minutes);
        }

        if (timeSpan.TotalHours < 24)
        {
            return string.Format(HoursAgo, timeSpan.Hours);
        }

        if (timeSpan.TotalDays < 7)
        {
            return string.Format(DaysAgo, timeSpan.Days);
        }

        if (timeSpan.TotalDays < 30)
        {
            return string.Format(WeeksAgo, timeSpan.Days / 7);
        }

        if (timeSpan.TotalDays < 365)
        {
            return string.Format(MonthsAgo, timeSpan.Days / 30);
        }

        if (timeSpan.TotalDays >= 365)
        {
            return string.Format(YearsAgo, timeSpan.Days / 365);
        }

        return $"{value}";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
