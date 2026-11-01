using System.Globalization;
using Avalonia.Data.Converters;

namespace DAORework.App.Views;

/// <summary>
/// Shows a <see cref="DateTimeOffset"/> or a UTC <see cref="DateTime"/> as <see cref="FriendlyTime"/> does, against
/// <see cref="Clock"/>. The App sets the clock from its services when it starts; without one, the date and time are
/// shown as they are.
/// </summary>
public sealed class FriendlyTimeConverter : IValueConverter
{
    public static FriendlyTimeConverter Instance { get; } = new();

    /// <summary>Gets or sets the clock the times are measured against: the App's <see cref="TimeProvider"/>.</summary>
    public static TimeProvider? Clock { get; set; }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        DateTimeOffset? when = value switch
        {
            DateTimeOffset offset => offset,
            DateTime { Kind: DateTimeKind.Utc } utc => new DateTimeOffset(utc),
            _ => null,
        };
        if (when is not { } moment)
        {
            return value;
        }

        if (Clock is not { } clock)
        {
            return moment.ToString("d MMM yyyy, HH:mm", CultureInfo.InvariantCulture);
        }

        return FriendlyTime.Of(TimeZoneInfo.ConvertTime(moment, clock.LocalTimeZone), clock.GetLocalNow());
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
