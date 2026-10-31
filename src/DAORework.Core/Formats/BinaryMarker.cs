using System.Text;

namespace DAORework.Core.Formats;

/// <summary>
/// Finds a marker string compiled into a binary. The marker must be a whole NUL-terminated string, which keeps a
/// longer string that merely starts with it from matching; the NUL-terminated string after it is returned, which
/// is where builds such as DXVK keep their version ("DXVK: " then "v2.3").
/// </summary>
public static class BinaryMarker
{
    /// <summary>The longest text after a marker that is taken as a version; anything longer is not one.</summary>
    public const int MaxFollowingTextLength = 64;

    public static bool TryFind(ReadOnlySpan<byte> data, string marker, out string? followingText)
    {
        ArgumentException.ThrowIfNullOrEmpty(marker);
        followingText = null;
        var needle = Encoding.ASCII.GetBytes(marker + "\0");
        var offset = 0;
        while (offset < data.Length)
        {
            var index = data[offset..].IndexOf(needle);
            if (index < 0)
            {
                return false;
            }

            var start = offset + index;
            if (start == 0 || data[start - 1] == 0)
            {
                followingText = FollowingText(data[(start + needle.Length)..]);
                return true;
            }

            offset = start + 1;
        }

        return false;
    }

    private static string? FollowingText(ReadOnlySpan<byte> rest)
    {
        var start = rest.IndexOfAnyExcept((byte)0);
        if (start < 0)
        {
            return null;
        }

        var text = rest[start..];
        var end = text.IndexOf((byte)0);
        if (end <= 0 || end > MaxFollowingTextLength)
        {
            return null;
        }

        foreach (var b in text[..end])
        {
            if (b is < 0x20 or > 0x7E)
            {
                return null;
            }
        }

        return Encoding.ASCII.GetString(text[..end]);
    }
}
