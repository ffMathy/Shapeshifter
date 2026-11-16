using System.Globalization;
using Shapeshifter.Core.Text;

namespace Shapeshifter.Core.Tests.Text;

public sealed class RelativeTimeTests
{
    // A mid-November afternoon in the local time zone, away from daylight saving changes.
    private static readonly DateTimeOffset Now = Local(2026, 11, 16, 15, 0);

    private static DateTimeOffset Local(int year, int month, int day, int hour, int minute)
    {
        var time = new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Unspecified);
        return new DateTimeOffset(time, TimeZoneInfo.Local.GetUtcOffset(time));
    }

    private static string Describe(DateTimeOffset time) => RelativeTime.Describe(time, Now, CultureInfo.InvariantCulture);

    [Fact]
    public void WithinAMinute_IsJustNow()
    {
        Assert.Equal("Just now", Describe(Now));
        Assert.Equal("Just now", Describe(Now - TimeSpan.FromSeconds(59)));
    }

    [Fact]
    public void InTheFuture_IsJustNow()
    {
        // The clock may have been adjusted after the copy.
        Assert.Equal("Just now", Describe(Now + TimeSpan.FromMinutes(5)));
    }

    [Theory]
    [InlineData(1, "1 min ago")]
    [InlineData(5, "5 min ago")]
    [InlineData(59, "59 min ago")]
    public void WithinAnHour_CountsMinutes(int minutes, string expected)
    {
        Assert.Equal(expected, Describe(Now - TimeSpan.FromMinutes(minutes)));
    }

    [Theory]
    [InlineData(60, "1 h ago")]
    [InlineData(150, "2 h ago")]
    [InlineData(14 * 60 + 59, "14 h ago")]
    public void EarlierToday_CountsHours(int minutes, string expected)
    {
        Assert.Equal(expected, Describe(Now - TimeSpan.FromMinutes(minutes)));
    }

    [Fact]
    public void TheDayBefore_IsYesterday_EvenWhenLessThanADayAgo()
    {
        Assert.Equal("Yesterday", Describe(Local(2026, 11, 15, 23, 30)));
        Assert.Equal("Yesterday", Describe(Local(2026, 11, 15, 0, 5)));
    }

    [Theory]
    [InlineData(14, "2 days ago")]
    [InlineData(10, "6 days ago")]
    public void WithinAWeek_CountsDays(int day, string expected)
    {
        Assert.Equal(expected, Describe(Local(2026, 11, day, 12, 0)));
    }

    [Fact]
    public void OlderThisYear_ShowsMonthAndDay()
    {
        Assert.Equal("November 02", Describe(Local(2026, 11, 2, 12, 0)));
    }

    [Fact]
    public void FromAnotherYear_ShowsTheFullDate()
    {
        Assert.Equal("11/16/2025", Describe(Local(2025, 11, 16, 12, 0)));
    }
}
