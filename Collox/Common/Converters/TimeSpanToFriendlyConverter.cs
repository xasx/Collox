using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.UI.Xaml.Data;

namespace Collox.Common.Converters;
internal class TimeSpanToFriendlyConverter : IValueConverter
{
    

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var timeSpan = (TimeSpan)value;
        if (timeSpan.TotalSeconds < 60)
        {
            return $"{timeSpan.Seconds} seconds ago";
        }
        if (timeSpan.TotalMinutes < 60)
        {
            return $"{timeSpan.Minutes} minutes ago";
        }
        if (timeSpan.TotalHours < 24)
        {
            return $"{timeSpan.Hours} hours ago";
        }
        if (timeSpan.TotalDays < 7)
        {
            return $"{timeSpan.Days} days ago";
        }
        if (timeSpan.TotalDays < 30)
        {
            return $"{timeSpan.Days / 7} weeks ago";
        }
        if (timeSpan.TotalDays < 365)
        {
            return $"{timeSpan.Days / 30} months ago";
        }
        if (timeSpan.TotalDays >= 365)
        {
            return $"{timeSpan.Days / 365} years ago";
        }

        return $"{value}"; 
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
