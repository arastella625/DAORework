using System.Text;
using System.Text.RegularExpressions;

namespace DAORework.Core.Mods.Library;

/// <summary>
/// A library id (ADR-0012): the name in lower case with hyphens, then the first 8 hex digits of the source's SHA-256.
/// The same download always gets the same id; two releases of a mod get different ones.
/// </summary>
public static class ModIds
{
    public const int MaxNameLength = 40;

    /// <summary>
    /// A name to show for a download without a manifest: its file name, without the "-1234-1-0-1600000000" Nexus adds
    /// (mod id, version, upload time), so "Better Hair-1234-1-0-1600000000.zip" reads "Better Hair".
    /// </summary>
    public static string NameFromFileName(string fileNameWithoutExtension)
    {
        ArgumentNullException.ThrowIfNull(fileNameWithoutExtension);
        var trimmed = Regex.Replace(fileNameWithoutExtension, @"(-\d+)+$", string.Empty, RegexOptions.None, TimeSpan.FromSeconds(1)).Trim();
        return trimmed.Length == 0 ? fileNameWithoutExtension : trimmed;
    }

    public static string Create(string name, string sourceSha256)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceSha256);
        var slug = new StringBuilder();
        foreach (var c in name.ToLowerInvariant())
        {
            if (char.IsAsciiLetterOrDigit(c))
            {
                slug.Append(c);
            }
            else if (slug.Length > 0 && slug[^1] != '-')
            {
                slug.Append('-');
            }
        }

        var trimmed = slug.ToString(0, Math.Min(slug.Length, MaxNameLength)).Trim('-');
        return $"{(trimmed.Length == 0 ? "mod" : trimmed)}-{sourceSha256[..8].ToLowerInvariant()}";
    }
}
