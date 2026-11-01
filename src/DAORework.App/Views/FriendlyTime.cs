using System.Globalization;

namespace DAORework.App.Views;

/// <summary>
/// A moment as a person says it (docs/VOICE.md rule 5): <i>Just now</i>, <i>5 minutes ago</i>, <i>Today, 18:05</i>,
/// <i>Yesterday, 21:40</i>, <i>Tuesday, 19:05</i>, <i>3 Oct, 09:12</i>, <i>3 Oct 2025</i>. Both times are local. Pure.
/// </summary>
public static class FriendlyTime
{
    public static string Of(DateTimeOffset when, DateTimeOffset now)
    {
        var ago = now - when;
        if (ago >= TimeSpan.Zero && ago < TimeSpan.FromMinutes(1))
        {
            return "Just now";
        }

        if (ago >= TimeSpan.Zero && ago < TimeSpan.FromHours(1))
        {
            var minutes = (int)ago.TotalMinutes;
            return minutes == 1 ? "1 minute ago" : $"{minutes} minutes ago";
        }

        var days = (now.Date - when.Date).Days;
        var time = when.ToString("HH:mm", CultureInfo.InvariantCulture);
        return days switch
        {
            0 => $"Today, {time}",
            1 => $"Yesterday, {time}",
            > 1 and < 7 => $"{when.ToString("dddd", CultureInfo.InvariantCulture)}, {time}",
            _ when when.Year == now.Year => $"{when.ToString("d MMM", CultureInfo.InvariantCulture)}, {time}",
            _ => when.ToString("d MMM yyyy", CultureInfo.InvariantCulture),
        };
    }
}
