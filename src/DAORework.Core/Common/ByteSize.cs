using System.Globalization;

namespace DAORework.Core.Common;

/// <summary>A non-negative number of bytes with one human-readable rendering (audit B2).</summary>
public readonly record struct ByteSize
{
    private static readonly string[] _units = ["B", "KB", "MB", "GB", "TB"];

    public ByteSize(long bytes)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(bytes);
        Bytes = bytes;
    }

    public long Bytes { get; }

    public static ByteSize Zero => new(0);

    /// <summary>Renders as "1.5 MB": powers of 1024, at most two decimals, invariant culture.</summary>
    public override string ToString()
    {
        double size = Bytes;
        var unitIndex = 0;

        while (size >= 1024 && unitIndex < _units.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }

        return string.Create(CultureInfo.InvariantCulture, $"{size:0.##} {_units[unitIndex]}");
    }
}
