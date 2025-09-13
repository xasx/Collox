using System.Text;
using Microsoft.UI.Xaml.Data;

namespace Collox.Common.Converters;

public partial class TimeSpanToFriendlyConverter : IValueConverter
{
    // Keep original properties for XAML binding
    public string SecondsAgo { get; set; } = "{0} seconds ago";
    public string MinutesAgo { get; set; } = "{0} minutes ago";
    public string HoursAgo { get; set; } = "{0} hours ago";
    public string DaysAgo { get; set; } = "{0} days ago";
    public string WeeksAgo { get; set; } = "{0} weeks ago";
    public string MonthsAgo { get; set; } = "{0} months ago";
    public string YearsAgo { get; set; } = "{0} years ago";

    // Cached CompositeFormat instances - lazy-loaded for performance
    private CompositeFormat _secondsFormat;
    private CompositeFormat _minutesFormat;
    private CompositeFormat _hoursFormat;
    private CompositeFormat _daysFormat;
    private CompositeFormat _weeksFormat;
    private CompositeFormat _monthsFormat;
    private CompositeFormat _yearsFormat;

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is not TimeSpan timeSpan)
            return value?.ToString() ?? string.Empty;

        var totalSeconds = timeSpan.TotalSeconds;
        var totalMinutes = timeSpan.TotalMinutes;
        var totalHours = timeSpan.TotalHours;
        var totalDays = timeSpan.TotalDays;

        // Use if-else chain instead of switch expression to avoid pattern matching overhead
        if (totalSeconds < 60)
            return string.Format(null, GetSecondsFormat(), (int)totalSeconds);

        if (totalMinutes < 60)
            return string.Format(null, GetMinutesFormat(), (int)totalMinutes);

        if (totalHours < 24)
            return string.Format(null, GetHoursFormat(), (int)totalHours);

        if (totalDays < 7)
            return string.Format(null, GetDaysFormat(), (int)totalDays);

        if (totalDays < 30)
            return string.Format(null, GetWeeksFormat(), (int)(totalDays / 7));

        if (totalDays < 365)
            return string.Format(null, GetMonthsFormat(), (int)(totalDays / 30));

        return string.Format(null, GetYearsFormat(), (int)(totalDays / 365));
    }

    // Lazy-loading format getters - parse only when needed and cache
    private CompositeFormat GetSecondsFormat() =>
        _secondsFormat ??= CompositeFormat.Parse(SecondsAgo);

    private CompositeFormat GetMinutesFormat() =>
        _minutesFormat ??= CompositeFormat.Parse(MinutesAgo);

    private CompositeFormat GetHoursFormat() =>
        _hoursFormat ??= CompositeFormat.Parse(HoursAgo);

    private CompositeFormat GetDaysFormat() =>
        _daysFormat ??= CompositeFormat.Parse(DaysAgo);

    private CompositeFormat GetWeeksFormat() =>
        _weeksFormat ??= CompositeFormat.Parse(WeeksAgo);

    private CompositeFormat GetMonthsFormat() =>
        _monthsFormat ??= CompositeFormat.Parse(MonthsAgo);

    private CompositeFormat GetYearsFormat() =>
        _yearsFormat ??= CompositeFormat.Parse(YearsAgo);

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
