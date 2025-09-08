using System;
using System.Globalization;

public static class DateTimeExtensions
{
    public static string ToLocalizedString(this DateTime dateTime, CultureInfo culture)
    {
        string dateFormat = culture.DateTimeFormat.ShortDatePattern;
        string timeFormat = culture.DateTimeFormat.LongTimePattern;
        string format = $"{dateFormat} {timeFormat}";

        return dateTime.ToString(format, culture);
    }
}