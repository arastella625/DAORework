using System.IO.Abstractions;
using System.Xml;
using System.Xml.Linq;
using DAORework.Application.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Adoption;
using DAORework.Core.Mods.Library;

namespace DAORework.Infrastructure.Mods;

/// <summary>
/// Reads a documents folder for the adoption scan (ADR-0015): the add-ins <c>Settings\AddIns.xml</c> registers, the
/// files in each <c>AddIns</c> folder and each top-level <c>override</c> folder, the loose files in <c>override</c>,
/// and whether Vortex has left its <c>vortex.deployment*.json</c> in either. Never writes.
/// </summary>
public sealed class HandInstalledModsReader : IHandInstalledModsReader
{
    private readonly IFileSystem _fileSystem;

    public HandInstalledModsReader(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public async Task<Result<HandInstalledSnapshot>> ReadAsync(string documentsDirectory, CancellationToken cancellationToken = default)
    {
        try
        {
            var addIns = DocumentsPaths.Resolve(_fileSystem, documentsDirectory, GamePaths.AddInsDirectoryName);
            var overrideRoot = DocumentsPaths.Resolve(_fileSystem, documentsDirectory, ModLayout.OverridePrefix.TrimEnd('/'));
            var documents = _fileSystem.Path.GetFullPath(documentsDirectory);
            return Result.Success(new HandInstalledSnapshot(
                await RegisteredAsync(documentsDirectory, cancellationToken),
                Folders(documents, addIns),
                Folders(documents, overrideRoot),
                Files(documents, TopLevelFiles(overrideRoot).Where(f => !IsVortexManifest(f))))
            {
                VortexDeployedOverride = TopLevelFiles(overrideRoot).Any(IsVortexManifest),
                VortexDeployedAddIns = TopLevelFiles(addIns).Any(IsVortexManifest),
            });
        }
        catch (XmlException exception)
        {
            return Failed($"Settings/AddIns.xml could not be read: {exception.Message}");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return Failed(exception.Message);
        }
    }

    private static Result<HandInstalledSnapshot> Failed(string reason) =>
        Result.Failure<HandInstalledSnapshot>(new Error(DiagnosticCodes.ModAdoptScanFailed, $"The documents folder could not be read: {reason}", "Close the game and try again."));

    private static string? Title(XElement item) =>
        (string?)item.Element("Title")?.Attribute("DefaultText") ?? (string?)item.Attribute("Name");

    private bool IsVortexManifest(string path)
    {
        var name = _fileSystem.Path.GetFileName(path);
        return name.StartsWith("vortex.deployment", StringComparison.OrdinalIgnoreCase) && name.EndsWith(".json", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<List<RegisteredAddIn>> RegisteredAsync(string documents, CancellationToken cancellationToken)
    {
        var path = DocumentsPaths.Resolve(_fileSystem, documents, "Settings/AddIns.xml");
        if (!_fileSystem.File.Exists(path))
        {
            return [];
        }

        await using var stream = _fileSystem.File.OpenRead(path);
        var document = await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken);
        return document.Descendants("AddInItem")
            .Select(item => (string?)item.Attribute("UID") is { Length: > 0 } uid ? new RegisteredAddIn(uid, Title(item) ?? uid) : null)
            .OfType<RegisteredAddIn>()
            .ToList();
    }

    private IEnumerable<string> TopLevelFiles(string folder) =>
        _fileSystem.Directory.Exists(folder) ? _fileSystem.Directory.EnumerateFiles(folder) : [];

    private Dictionary<string, IReadOnlyList<HandInstalledFile>> Folders(string documents, string root)
    {
        var folders = new Dictionary<string, IReadOnlyList<HandInstalledFile>>(StringComparer.OrdinalIgnoreCase);
        if (_fileSystem.Directory.Exists(root))
        {
            foreach (var folder in _fileSystem.Directory.EnumerateDirectories(root))
            {
                folders[_fileSystem.Path.GetFileName(folder)] = Files(documents, _fileSystem.Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories));
            }
        }

        return folders;
    }

    private List<HandInstalledFile> Files(string documents, IEnumerable<string> paths) =>
        paths
            .Select(path => new HandInstalledFile(_fileSystem.Path.GetRelativePath(documents, path).Replace('\\', '/'), _fileSystem.FileInfo.New(path).Length))
            .OrderBy(f => f.Path, StringComparer.Ordinal)
            .ToList();
}
