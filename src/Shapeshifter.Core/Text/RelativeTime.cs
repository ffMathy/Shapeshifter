using System.Globalization;

namespace Shapeshifter.Core.Text;

/// <summary>Short, human descriptions of when something happened, such as "Just now" or "5 min ago".</summary>
public static class RelativeTime
{
    /// <summary>
    /// Describes <paramref name="time"/> relative to <paramref name="now"/>: "Just now" within a minute, then minutes,
    /// hours, "Yesterday", days for the last week, and finally the date (with the year only when it differs).
    /// </summary>
    public static string Describe(DateTimeOffset time, DateTimeOffset now, CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentCulture;
        var elapsed = now - time;
        if (elapsed < TimeSpan.FromMinutes(1))
        {
            // Also covers clocks that went backwards.
            return "Just now";
        }

        if (elapsed < TimeSpan.FromHours(1))
        {
            return string.Create(culture, $"{(int)elapsed.TotalMinutes} min ago");
        }

        var localTime = time.ToLocalTime();
        var localNow = now.ToLocalTime();
        if (elapsed < TimeSpan.FromHours(24) && localTime.Date == localNow.Date)
        {
            return string.Create(culture, $"{(int)elapsed.TotalHours} h ago");
        }

        var days = (localNow.Date - localTime.Date).Days;
        if (days <= 1)
        {
            return "Yesterday";
        }

        if (days < 7)
        {
            return string.Create(culture, $"{days} days ago");
        }

        return localTime.Year == localNow.Year
            ? localTime.ToString(culture.DateTimeFormat.MonthDayPattern, culture)
            : localTime.ToString("d", culture);
    }
}
