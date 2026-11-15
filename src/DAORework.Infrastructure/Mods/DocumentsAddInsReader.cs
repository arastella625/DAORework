using System.IO.Abstractions;
using DAORework.Application.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.AddIns;
using DAORework.Formats.Erf;

namespace DAORework.Infrastructure.Mods;

/// <summary>
/// Reads the add-ins the game loads from a documents folder (P2.5-23). For each one <c>AddIns.xml</c> enables whose
/// folder is in <c>AddIns</c>, it lists the names under <c>core</c> and <c>module</c>: a loose file by its own name, an
/// <c>.erf</c> or <c>.rim</c> by the names in its table of contents. <c>data_tools</c> folders hold the toolset's files,
/// which the game never loads, so they're left out. An archive that can't be read is noted, not a stop. Never writes.
/// </summary>
public sealed class DocumentsAddInsReader : IDocumentsAddInsReader
{
    private static readonly HashSet<string> _archives = new(StringComparer.OrdinalIgnoreCase) { ".erf", ".rim" };

    private readonly IFileSystem _fileSystem;

    public DocumentsAddInsReader(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public async Task<Result<DocumentsAddIns>> ReadAsync(string documentsDirectory, CancellationToken cancellationToken = default)
    {
        var listFile = DocumentsPaths.Resolve(_fileSystem, documentsDirectory, "Settings/AddIns.xml");
        try
        {
            if (!_fileSystem.File.Exists(listFile))
            {
                return Result.Success(DocumentsAddIns.Empty);
            }

            if (!AddInsList.Parse(await _fileSystem.File.ReadAllTextAsync(listFile, cancellationToken)).TryGetValue(out var listed, out var error))
            {
                return Result.Failure<DocumentsAddIns>(error);
            }

            var documents = _fileSystem.Path.GetFullPath(documentsDirectory);
            var unreadable = new List<string>();
            var contents = listed
                .Select(addIn => (AddIn: addIn, Folder: Folder(documents, addIn.Uid)))
                .Where(a => a.Folder is not null)
                .Select(a => new AddInContents(a.AddIn, Names(documents, a.Folder!, "core", unreadable), Names(documents, a.Folder!, "module", unreadable)))
                .ToList();
            return Result.Success(new DocumentsAddIns(listed, contents, unreadable));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return Result.Failure<DocumentsAddIns>(new Error(
                DiagnosticCodes.AddInsListUnreadable,
                $"Couldn't read the add-ins in {documentsDirectory}: {exception.Message}",
                "Close the game, then check again."));
        }
    }

    /// <returns>The add-in's folder in <c>AddIns</c>, matched without case, or null when it isn't there or its UID isn't a plain name.</returns>
    private string? Folder(string documents, string uid)
    {
        var addIns = GamePaths.AddInsDirectory(documents);
        if (uid.IndexOfAny(_fileSystem.Path.GetInvalidFileNameChars()) >= 0 || uid is "." or ".." || !_fileSystem.Directory.Exists(addIns))
        {
            return null;
        }

        return _fileSystem.Directory.EnumerateDirectories(addIns)
            .FirstOrDefault(d => string.Equals(_fileSystem.Path.GetFileName(d), uid, StringComparison.OrdinalIgnoreCase));
    }

    private List<string> Names(string documents, string addInFolder, string part, List<string> unreadable)
    {
        var root = _fileSystem.Directory.EnumerateDirectories(addInFolder)
            .FirstOrDefault(d => string.Equals(_fileSystem.Path.GetFileName(d), part, StringComparison.OrdinalIgnoreCase));
        if (root is null)
        {
            return [];
        }

        var names = new List<string>();
        foreach (var file in _fileSystem.Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories).Where(f => !InToolsFolder(root, f)))
        {
            if (!_archives.Contains(_fileSystem.Path.GetExtension(file)))
            {
                names.Add(_fileSystem.Path.GetFileName(file));
            }
            else if (ArchiveNames(file) is { } inside)
            {
                names.AddRange(inside);
            }
            else
            {
                unreadable.Add(_fileSystem.Path.GetRelativePath(documents, file).Replace('\\', '/'));
            }
        }

        return names;
    }

    private bool InToolsFolder(string root, string file) =>
        _fileSystem.Path.GetRelativePath(root, file).Split('/', '\\').SkipLast(1).Contains("data_tools", StringComparer.OrdinalIgnoreCase);

    private IEnumerable<string>? ArchiveNames(string file)
    {
        try
        {
            using var stream = _fileSystem.File.OpenRead(file);
            return [.. ErfReader.ReadTableOfContents(stream).Entries.Select(e => e.Name)];
        }
        catch (Exception exception) when (exception is InvalidDataException or EndOfStreamException or IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }
}
