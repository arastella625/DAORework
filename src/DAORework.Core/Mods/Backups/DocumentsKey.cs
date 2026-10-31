using System.Security.Cryptography;
using System.Text;

namespace DAORework.Core.Mods.Backups;

/// <summary>
/// Names a documents folder in DAORework's own folders (ADR-0012): the first 16 hex digits of the SHA-256 of its
/// normalized path, so the key has no characters a folder name cannot hold and the same folder always gets the same key.
/// </summary>
public static class DocumentsKey
{
    public static string For(string documentsDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(documentsDirectory);
        var normalized = documentsDirectory.Replace('\\', '/').TrimEnd('/').ToUpperInvariant();
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)))[..16];
    }
}
