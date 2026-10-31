using DAORework.Core.Mods.Library;

namespace DAORework.Core.Mods.Safety;

/// <summary>
/// Which files in a mod are programs (ADR-0018): files Windows runs or loads. A Dragon Age mod is game data, so a
/// program in one is a helper the author bundled or something worse; it is flagged and never deployed. The one list
/// of extensions lives here.
/// </summary>
public static class ProgramFiles
{
    private static readonly HashSet<string> _extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".exe", ".dll", ".com", ".scr", ".cpl", ".ocx", ".sys", ".msi", ".msp", ".bat", ".cmd", ".ps1", ".psm1",
        ".vbs", ".vbe", ".js", ".jse", ".wsf", ".wsh", ".hta", ".jar", ".lnk", ".url", ".reg",
    };

    /// <summary>Whether a library file is a program: classified so at import, or named like one.</summary>
    public static bool IsProgram(LibraryModFile file)
    {
        ArgumentNullException.ThrowIfNull(file);
        return file.Kind == ModFileKind.Program || IsProgram(file.RelativePath);
    }

    /// <summary>Whether a file's first bytes are a Windows executable header (<c>MZ</c>), which a renamed program still has.</summary>
    public static bool HasExecutableHeader(ReadOnlySpan<byte> start) => start.Length >= 2 && start[0] == (byte)'M' && start[1] == (byte)'Z';

    /// <summary>Classifies a file from its path and its first bytes.</summary>
    public static ModFileKind Classify(string path, ReadOnlySpan<byte> start) =>
        IsProgram(path) || HasExecutableHeader(start) ? ModFileKind.Program : ModFileKind.Data;

    /// <summary>Whether <paramref name="path"/> (any separators) names a program by its extension.</summary>
    public static bool IsProgram(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        var name = path[(path.LastIndexOfAny(['/', '\\']) + 1)..];
        var dot = name.LastIndexOf('.');
        return dot >= 0 && _extensions.Contains(name[dot..]);
    }
}
