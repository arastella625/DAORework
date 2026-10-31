using Avalonia.Data.Converters;

namespace DAORework.App.Views;

/// <summary>Shows a warning line only when a count is above zero.</summary>
public static class CountConverters
{
    public static FuncValueConverter<int, bool> IsPositive { get; } = new(count => count > 0);
}
