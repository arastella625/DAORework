using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Fixes;
using DAORework.Core.Formats;
using DAORework.Core.Mods.Library;

namespace DAORework.Core.Mods.Safety;

/// <summary>
/// What may go into <c>bin_ship</c> from a mod that runs inside the game (ADR-0033 §2), checked when it is planned. A
/// refusal blocks the whole mod and names the file. Pure: the caller reads the files' headers and the install folder.
/// </summary>
public static class GameProgramRules
{
    /// <summary>
    /// Only DLLs, whatever their extension (so Luma's <c>.addon</c> passes and a renamed <c>.exe</c> doesn't), no script
    /// or installer, at least one 32-bit DLL, and every path inside <c>bin_ship</c>. Returns the 64-bit DLLs, which may
    /// go in beside a 32-bit one and are named under Details.
    /// </summary>
    public static Result<IReadOnlyList<string>> CheckFiles(string modName, IReadOnlyList<GameProgramFileFacts> files)
    {
        ArgumentNullException.ThrowIfNull(files);
        var refused = files.Select(Refusal).OfType<string>().ToList();
        var dlls = files.Where(f => f.Image is { IsDll: true }).ToList();
        if (refused.Count == 0 && !dlls.Exists(f => f.Image!.Machine == PortableExecutableMachine.X86))
        {
            refused.Add($"None of {modName}'s DLLs is 32-bit, and the game is 32-bit, so it couldn't load them.");
        }

        return refused.Count > 0
            ? Result.Failure<IReadOnlyList<string>>(new Error(
                DiagnosticCodes.ModSafetyProgramRefused,
                $"{modName} can't be turned on. {string.Join(" ", refused)}",
                "Nothing was changed, and it stays in your Library."))
            : Result.Success<IReadOnlyList<string>>([.. dlls.Where(f => f.Image!.Machine == PortableExecutableMachine.X64).Select(f => f.File.TargetPath)]);
    }

    /// <summary>
    /// Never over another file (ADR-0033 §2): a file on disk that isn't this mod's as it placed it, by the record and its
    /// hash together, blocks the plan. Another mod's or a fix's file says which to turn off first; this mod's own changed
    /// file says to turn the mod off; any other says it was not placed by DAORework. Null when every path is free or as placed.
    /// </summary>
    public static Error? CheckTargets(string modId, string modName, IReadOnlyList<GameProgramFile> targets, InstallState state, Func<string, string> nameOf)
    {
        ArgumentNullException.ThrowIfNull(targets);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(nameOf);
        var taken = targets.Select(t => Taken(modId, modName, t.TargetPath, state, nameOf)).OfType<string>().ToList();
        return taken.Count == 0
            ? null
            : new Error(
                DiagnosticCodes.ModProgramPathTaken,
                $"{modName} can't be turned on: {string.Join(" ", taken)}",
                "Nothing was changed. There's no Arrange for the game folder: only one file can have each name there.");
    }

    private static string? Refusal(GameProgramFileFacts facts)
    {
        var path = facts.File.TargetPath;
        if (!ModPath.IsSafe(path) || !path.StartsWith(GamePaths.BinShipDirectoryName + "/", StringComparison.OrdinalIgnoreCase))
        {
            return $"{facts.File.File.RelativePath} would go outside the game's {GamePaths.BinShipDirectoryName} folder.";
        }

        return facts switch
        {
            { HasExecutableHeader: true, Image: null } => $"{path} looks like a program, but DAORework can't read what kind.",
            { Image.IsDll: false } => $"{path} is a program that runs on its own, not a DLL. DAORework only places DLLs in your game folder.",
            { HasExecutableHeader: false } when ProgramFiles.IsProgram(path) => $"{path} is a script, an installer or a program DAORework can't read. Only DLLs go in.",
            _ => null,
        };
    }

    private static string? Taken(string modId, string modName, string path, InstallState state, Func<string, string> nameOf)
    {
        if (!state.Hashes.TryGetValue(path, out var current))
        {
            return null;
        }

        var recorded = state.Record.Files.FirstOrDefault(f => string.Equals(f.TargetPath, path, StringComparison.OrdinalIgnoreCase));
        if (recorded is null)
        {
            return $"{path} is already in your game folder, and DAORework didn't put it there. It's left alone: move it out yourself, then try again.";
        }

        if (!string.Equals(recorded.ModId, modId, StringComparison.OrdinalIgnoreCase))
        {
            return FixIds.IsFixEntry(recorded.ModId)
                ? $"{path} is {FixName(recorded.ModId)}'s, a fix DAORework placed. Turn it off on the Fixes page first."
                : $"{path} is {nameOf(recorded.ModId)}'s, which is on. Turn it off first: a new version of a mod is a separate mod in your Library.";
        }

        // This mod's own file, changed since it was placed: an edited .ini, say. It is never replaced without asking.
        return string.Equals(current, recorded.Sha256, StringComparison.OrdinalIgnoreCase)
            ? null
            : $"{path} has changed since {modName} placed it. To put the mod back as it came, turn it off first: the changed file stays where it is.";
    }

    // DXVK's d3d9.dll is the one fix file in bin_ship a mod like this can want; the 4 GB patch only changes daorigins.exe.
    private static string FixName(string libraryId) =>
        FixIds.DisplayName(FixIds.IsEntryOf(libraryId, FixIds.Dxvk) ? FixIds.Dxvk : FixIds.LargeAddressAware);
}
