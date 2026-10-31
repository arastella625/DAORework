namespace DAORework.Core.Mods.Library;

/// <summary>
/// The one rule for a path inside a mod: relative, <c>/</c>-separated, no empty, <c>.</c> or <c>..</c> segment and no
/// drive. It keeps an archive entry or a library file from ever pointing outside its folder ("zip slip").
/// </summary>
public static class ModPath
{
    public static bool IsSafe(string? path) =>
        !string.IsNullOrWhiteSpace(path)
        && !path.Contains('\\', StringComparison.Ordinal)
        && !path.Contains(':', StringComparison.Ordinal)
        && !path.StartsWith('/')
        && path.Split('/').All(segment => segment is not ("" or "." or ".."));
}
