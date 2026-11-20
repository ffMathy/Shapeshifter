# When it was copied, in words

```ail
node RelativeTime : fn   level: system
  why   A time of day means little in a list; "5 min ago" tells at once which of
        two similar copies is the recent one (#444, PR #623).   <- HistoryRow
  what
    Describe(time: time, now: time, culture: CultureInfo or nothing = nothing) -> text
      culture defaults to the current display language; words come from the translations of that culture, numbers and dates are formatted in it
    In the examples, times are local, the culture is en-US, and "now" is 2026-11-20 15:00.
    examples
      Describe(now − 30 s, now) => "Just now"
      Describe(now + 5 min, now) => "Just now"; a clock that went backwards
      Describe(now − 59 s, now) => "Just now"
      Describe(now − 1 min, now) => "1 min ago"
      Describe(now − 59 min 59 s, now) => "59 min ago"
      Describe(now − 2 h 30 min, now) => "2 h ago"; whole hours, rounded down
      Describe(2026-11-20 00:30, now 2026-11-20 23:50) => "23 h ago"
      Describe(2026-11-19 23:00, now 2026-11-20 01:30) => "Yesterday"; less than a day, but another date
      Describe(2026-11-19 09:00, now) => "Yesterday"
      Describe(2026-11-17 09:00, now) => "3 days ago"
      Describe(2026-11-14 09:00, now) => "6 days ago"
      Describe(2026-11-13 09:00, now) => "November 13"; the culture's month and day
      Describe(2025-11-13 09:00, now) => "11/13/2025"; the culture's short date when the year differs
      Describe(now − 5 min, now, de) => "vor 5 Min."
      Describe(now − 30 s, now, de) => "Gerade eben"
  how
    decide minutes under an hour, hours only on the same date, then "Yesterday", days under a week, then the date
      because short words stay readable in the narrow end column of a row (PR #623)
    decide the date shows its year only when it is not this year
      because "unknown, confirm with the owner"
  sources
    src/Shapeshifter.Core/Text/RelativeTime.cs
    src/Shapeshifter.Core/Localization/Strings.resx: TimeJustNow, TimeMinutesAgo, TimeHoursAgo, TimeYesterday, TimeDaysAgo
    issue #444; PR #623 "Show copy times and pins in the picker"
    driven on Linux 2026-11-20: "Just now", then "5 min ago" and "8 min ago" on the same items; German "Gerade eben"
```
