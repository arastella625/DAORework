using System.IO.Abstractions;
using System.Xml;
using System.Xml.Linq;
using DAORework.Application.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Library;
using DAORework.Core.Mods.Planning;

namespace DAORework.Infrastructure.Mods;

/// <summary>Reads the documents folder for the planner (ROADMAP P2-06): hashes, hand-placed override files, registered add-ins. Never writes.</summary>
public sealed class DocumentsScanner : IDocumentsScanner
{
    private readonly IFileSystem _fileSystem;

    public DocumentsScanner(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public async Task<Result<DocumentsSnapshot>> ScanAsync(string documentsDirectory, IReadOnlyCollection<string> paths, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(paths);
        try
        {
            var hashes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var relative in paths)
            {
                var full = DocumentsPaths.Resolve(_fileSystem, documentsDirectory, relative);
                if (_fileSystem.File.Exists(full))
                {
                    hashes[relative] = await FileHashes.ComputeAsync(_fileSystem, full, cancellationToken);
                }
            }

            return Result.Success(new DocumentsSnapshot(hashes, UnmanagedOverrideFiles(documentsDirectory), await RegisteredAddInsAsync(documentsDirectory, cancellationToken)));
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

    private static Result<DocumentsSnapshot> Failed(string reason) =>
        Result.Failure<DocumentsSnapshot>(new Error(DiagnosticCodes.ModScanFailed, $"The documents folder could not be read: {reason}", "Close the game and try again."));

    private List<string> UnmanagedOverrideFiles(string documents)
    {
        var root = DocumentsPaths.Resolve(_fileSystem, documents, ModLayout.OverridePrefix.TrimEnd('/'));
        if (!_fileSystem.Directory.Exists(root))
        {
            return [];
        }

        var owned = ModLayout.OwnedOverrideFolder + "/";
        return _fileSystem.Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Select(f => _fileSystem.Path.GetRelativePath(root, f).Replace('\\', '/'))
            .Where(f => !f.StartsWith(owned, StringComparison.OrdinalIgnoreCase))
            .Order(StringComparer.Ordinal)
            .ToList();
    }

    private async Task<List<string>> RegisteredAddInsAsync(string documents, CancellationToken cancellationToken)
    {
        var path = DocumentsPaths.Resolve(_fileSystem, documents, "Settings/AddIns.xml");
        if (!_fileSystem.File.Exists(path))
        {
            return [];
        }

        await using var stream = _fileSystem.File.OpenRead(path);
        var document = await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken);
        return document.Descendants("AddInItem").Select(e => (string?)e.Attribute("UID")).OfType<string>().ToList();
    }
}
