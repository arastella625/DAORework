using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Library;

namespace DAORework.Core.Mods.Safety;

/// <summary>
/// What is said about a library mod's safety (ADR-0018), worded once for the CLI and the window: its antivirus
/// verdict, the program files it holds, and how its download compares with the catalog's known downloads.
/// </summary>
public static class LibraryEntryNotes
{
    /// <summary>The antivirus verdict, never worded as "safe".</summary>
    public static string Scan(LibraryEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return entry.Scan.Status == MalwareScanStatus.NoThreatFound ? $"Antivirus: {entry.Scan.Detail}" : $"Not scanned: {entry.Scan.Detail}";
    }

    /// <summary>The program files the mod holds, or null when it holds none.</summary>
    public static string? Programs(LibraryEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        var programs = entry.Mod.Files.Where(ProgramFiles.IsProgram).Select(f => f.RelativePath).ToList();
        return programs.Count == 0
            ? null
            : $"It holds {programs.Count} program file(s) ({string.Join(", ", programs)}). They stay in the library and are never installed into the game's folders.";
    }

    /// <summary>A known download, or a known mod imported from a folder; null otherwise.</summary>
    public static string? Download(DownloadCheck check)
    {
        ArgumentNullException.ThrowIfNull(check);
        return check switch
        {
            { Kind: DownloadCheckKind.Matches, Item: { } item } => $"Matches the known download of {item.Name} {check.Version}.",
            { Kind: DownloadCheckKind.FromAFolder, Item: { } item } =>
                $"It was imported from a folder, so it cannot be compared with the known downloads of {item.Name}. Import the download itself to check it.",
            _ => null,
        };
    }

    /// <summary>The warning for a download that differs from every known one of its mod; null otherwise.</summary>
    public static DiagnosticIssue? DownloadWarning(LibraryEntry entry, DownloadCheck check)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(check);
        return check is { Kind: DownloadCheckKind.Differs, Item: { } item }
            ? new DiagnosticIssue(
                DiagnosticSeverity.Warning,
                DiagnosticCodes.ModSafetyDownloadDiffers,
                $"{entry.SourceName} differs from the known downloads of {item.Name}.",
                $"Unless it is a newer version, get it from {item.Source}")
            : null;
    }
}
