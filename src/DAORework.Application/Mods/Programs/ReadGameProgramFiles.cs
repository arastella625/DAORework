using DAORework.Application.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Formats;
using DAORework.Core.Mods.Library;
using DAORework.Core.Mods.Safety;

namespace DAORework.Application.Mods.Programs;

/// <summary>
/// Reads a mod that runs inside the game from the library before it is planned (ADR-0033 §2): each file must still
/// have the hash it was added with, its header says whether it is a program and which kind, and its programs are
/// scanned again, since the antivirus may know more than it did at import. Only a clean scan lets them through.
/// </summary>
public sealed class ReadGameProgramFiles
{
    /// <summary>Enough for any real DOS stub and PE header; the PE offset is almost always under 0x100.</summary>
    private const int HeaderBytes = 4096;

    private readonly ILibraryFileReader _reader;
    private readonly IMalwareScanner _scanner;

    public ReadGameProgramFiles(ILibraryFileReader reader, IMalwareScanner scanner)
    {
        _reader = reader;
        _scanner = scanner;
    }

    public async Task<Result<IReadOnlyList<GameProgramFileFacts>>> ExecuteAsync(LibraryMod mod, IReadOnlyList<GameProgramFile> targets, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mod);
        ArgumentNullException.ThrowIfNull(targets);
        var facts = new List<GameProgramFileFacts>();
        var programs = new List<ScanTarget>();
        foreach (var target in targets)
        {
            if (!(await _reader.ReadAsync(mod.Id, target.File, HeaderBytes, cancellationToken)).TryGetValue(out var read, out var error))
            {
                return Result.Failure<IReadOnlyList<GameProgramFileFacts>>(error);
            }

            if (!string.Equals(read.Sha256, target.File.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                return Refused(mod, $"Your Library's copy of {target.File.RelativePath} has changed since {mod.Name} was added.", "Add the mod again to put the download's own files back.");
            }

            var header = ProgramFiles.HasExecutableHeader(read.Start);
            var image = header && PortableExecutableImage.TryRead(read.Start, out var parsed) ? parsed : null;
            facts.Add(new GameProgramFileFacts(target, header, image));
            if (header || ProgramFiles.IsProgram(target.TargetPath))
            {
                programs.Add(new ScanTarget(target.TargetPath, read.Path, IsProgram: true));
            }
        }

        var scan = programs.Count == 0 ? null : await _scanner.ScanAsync(programs, cancellationToken);
        return scan?.Status switch
        {
            MalwareScanStatus.ThreatFound => Result.Failure<IReadOnlyList<GameProgramFileFacts>>(new Error(
                DiagnosticCodes.ModSafetyThreatFound,
                $"Your antivirus reported {mod.Name}: {scan.Detail}",
                "Nothing was changed. Remove the mod from your Library.")),
            MalwareScanStatus.NotScanned => Result.Failure<IReadOnlyList<GameProgramFileFacts>>(new Error(
                DiagnosticCodes.ModSafetyProgramNotScanned,
                $"{mod.Name} runs its own program inside the game, and no antivirus could check it: {scan.Detail}",
                "Nothing was changed. A mod like this is only placed once your antivirus has checked it.")),
            _ => Result.Success<IReadOnlyList<GameProgramFileFacts>>(facts),
        };
    }

    private static Result<IReadOnlyList<GameProgramFileFacts>> Refused(LibraryMod mod, string why, string next) =>
        Result.Failure<IReadOnlyList<GameProgramFileFacts>>(new Error(DiagnosticCodes.ModSafetyProgramRefused, $"{mod.Name} can't be turned on. {why}", next));
}
