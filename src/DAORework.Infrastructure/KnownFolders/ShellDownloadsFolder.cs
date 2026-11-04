using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace DAORework.Infrastructure.KnownFolders;

/// <summary>
/// The Downloads folder the Windows shell reports (ADR-0034 §2). .NET's <c>SpecialFolder</c> has none, and the folder can
/// be moved to any drive, so it is asked for by its known-folder id. Empty off Windows, or if the shell has no answer.
/// </summary>
[SuppressMessage(
    "Interoperability",
    "SYSLIB1054:Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time",
    Justification = "LibraryImport needs AllowUnsafeBlocks, which Infrastructure keeps off; as for AMSI (P2.5-35).")]
internal static class ShellDownloadsFolder
{
    private static readonly Guid _downloads = new("374DE290-123F-4565-9164-39C4925E467B");

    public static string Path()
    {
        if (!OperatingSystem.IsWindows())
        {
            return string.Empty;
        }

        var id = _downloads;
        return SHGetKnownFolderPath(ref id, 0, IntPtr.Zero, out var path) == 0 ? path ?? string.Empty : string.Empty;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern int SHGetKnownFolderPath(ref Guid folderId, uint flags, IntPtr token, [MarshalAs(UnmanagedType.LPWStr)] out string? path);
}
