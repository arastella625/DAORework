using System.IO.Abstractions;
using DAORework.Application.Abstractions;
using DAORework.Core.Catalog;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Library;
using DAORework.Core.Profiles;

namespace DAORework.Application.Diagnostics.Rules;

/// <summary>
/// Reports each DLL in <c>bin_ship</c> that is neither the game's nor in the install folder's record (ADR-0033 §6), so a
/// player with a hand-placed <c>dinput8.dll</c> is told why a mod that wants the same file is blocked. Report only.
/// </summary>
public sealed class BinShipProgramsRule : IDiagnosticRule
{
    private readonly IFileSystem _fileSystem;
    private readonly IDeploymentRecordStore _records;
    private readonly IKnownItemsCatalogSource _catalog;

    public BinShipProgramsRule(IFileSystem fileSystem, IDeploymentRecordStore records, IKnownItemsCatalogSource catalog)
    {
        _fileSystem = fileSystem;
        _records = records;
        _catalog = catalog;
    }

    public async Task<IReadOnlyList<DiagnosticIssue>> EvaluateAsync(RuntimeProfile profile, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);
        if (string.IsNullOrWhiteSpace(profile.InstallDirectory))
        {
            return [];
        }

        var folder = _fileSystem.Path.Combine(profile.InstallDirectory, GamePaths.BinShipDirectoryName);
        if (!(await _records.LoadAsync(profile.InstallDirectory, cancellationToken)).TryGetValue(out var record, out var error))
        {
            return [new DiagnosticIssue(DiagnosticSeverity.Warning, error.Code, error.Message, error.Recommendation)];
        }

        var recorded = record.Files.Select(f => f.TargetPath[(f.TargetPath.LastIndexOf('/') + 1)..]).ToHashSet(StringComparer.OrdinalIgnoreCase);
        List<string> names;
        try
        {
            names = [.. Unknown(folder).Where(name => !recorded.Contains(name)).Order(StringComparer.OrdinalIgnoreCase)];
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return [new DiagnosticIssue(DiagnosticSeverity.Warning, DiagnosticCodes.InstallProgramFound, $"The game's bin_ship folder could not be read: {exception.Message}")];
        }

        if (names.Count == 0)
        {
            return [];
        }

        var known = _catalog.Load().TryGetValue(out var catalog, out _) ? catalog.Items : [];
        return [new DiagnosticIssue(
            DiagnosticSeverity.Info,
            DiagnosticCodes.InstallProgramFound,
            $"The game's bin_ship folder holds {names.Count} program(s) DAORework did not place: {string.Join(", ", names.Select(n => Named(n, known)))}. They run when the game starts.",
            "DAORework leaves them alone. A mod that needs a file of the same name can't be turned on while one is there.")];
    }

    /// <summary>A file name counts as the item's only by a fingerprint with no marker: <c>d3d9.dll</c> alone doesn't say DXVK.</summary>
    private static string Named(string name, IReadOnlyList<KnownItem> known)
    {
        var path = $"{GamePaths.BinShipDirectoryName}/{name}";
        var item = known.FirstOrDefault(i => i.Fingerprints.Any(f =>
            f.Kind == FingerprintKind.GameFile && f.Marker is null && f.Value.Equals(path, StringComparison.OrdinalIgnoreCase)));
        return item is null ? name : $"{name} ({item.Name})";
    }

    private List<string> Unknown(string folder)
    {
        if (!_fileSystem.Directory.Exists(folder))
        {
            return [];
        }

        return [.. _fileSystem.Directory.EnumerateFiles(folder, "*.dll", SearchOption.TopDirectoryOnly)
            .Select(path => _fileSystem.Path.GetFileName(path))
            .Where(name => !GameOwnDlls.Contains(name))];
    }
}
