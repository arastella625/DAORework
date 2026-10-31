using Avalonia.Data.Converters;
using DAORework.Core.Common;

namespace DAORework.App.Views;

/// <summary>Shows a byte count the way the rest of DAORework does (<see cref="ByteSize"/>).</summary>
public static class ByteSizeConverter
{
    public static FuncValueConverter<long, string> Instance { get; } = new(bytes => new ByteSize(bytes).ToString());
}
