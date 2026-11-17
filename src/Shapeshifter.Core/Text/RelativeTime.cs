using System.Globalization;
using Shapeshifter.Core.Localization;

namespace Shapeshifter.Core.Text;

/// <summary>Short, human descriptions of when something happened, such as "Just now" or "5 min ago".</summary>
public static class RelativeTime
{
    /// <summary>
    /// Describes <paramref name="time"/> relative to <paramref name="now"/>: "Just now" within a minute, then minutes,
    /// hours, "Yesterday", days for the last week, and finally the date (with the year only when it differs), in the
    /// language and format of <paramref name="culture"/> (the current UI language and format when it is null).
    /// </summary>
    public static string Describe(DateTimeOffset time, DateTimeOffset now, CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentUICulture;
        var elapsed = now - time;
        if (elapsed < TimeSpan.FromMinutes(1))
        {
            // Also covers clocks that went backwards.
            return Localizer.Get(nameof(Strings.TimeJustNow), culture);
        }

        if (elapsed < TimeSpan.FromHours(1))
        {
            return string.Format(culture, Localizer.Get(nameof(Strings.TimeMinutesAgo), culture), (int)elapsed.TotalMinutes);
        }

        var localTime = time.ToLocalTime();
        var localNow = now.ToLocalTime();
        if (elapsed < TimeSpan.FromHours(24) && localTime.Date == localNow.Date)
        {
            return string.Format(culture, Localizer.Get(nameof(Strings.TimeHoursAgo), culture), (int)elapsed.TotalHours);
        }

        var days = (localNow.Date - localTime.Date).Days;
        if (days <= 1)
        {
            return Localizer.Get(nameof(Strings.TimeYesterday), culture);
        }

        if (days < 7)
        {
            return string.Format(culture, Localizer.Get(nameof(Strings.TimeDaysAgo), culture), days);
        }

        return localTime.Year == localNow.Year
            ? localTime.ToString(culture.DateTimeFormat.MonthDayPattern, culture)
            : localTime.ToString("d", culture);
    }
}
