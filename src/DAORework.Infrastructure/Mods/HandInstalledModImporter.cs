using System.IO.Abstractions;
using System.Xml;
using System.Xml.Linq;
using DAORework.Application.Abstractions;
using DAORework.Application.Common;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Adoption;
using DAORework.Core.Mods.Library;
using Microsoft.Extensions.Options;

namespace DAORework.Infrastructure.Mods;

/// <summary>
/// Imports a hand-installed mod into the library from where its files are (ADR-0015), through the same
/// <see cref="LibraryImporter"/> as <c>mods import</c>, recorded as adopted from its folder. An add-in's copy gets a
/// <c>Manifest.xml</c> with its <c>AddInItem</c> copied from <c>Settings\AddIns.xml</c> as it is, so it re-registers
/// exactly as it was. Reads the documents folder; never writes it.
/// </summary>
public sealed class HandInstalledModImporter : IAdoptionImporter
{
    private readonly IFileSystem _fileSystem;
    private readonly LibraryImporter _importer;

    /// <summary>The adopted files go through <paramref name="scanner"/>, the antivirus scan (ADR-0018); without one, they are recorded as not scanned.</summary>
    public HandInstalledModImporter(IFileSystem fileSystem, IOptions<DaoReworkOptions> options, TimeProvider clock, IMalwareScanner? scanner = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        _fileSystem = fileSystem;
        _importer = new LibraryImporter(fileSystem, options.Value.LibraryDirectory, clock, scanner);
    }

    public async Task<Result<LibraryEntry>> ImportAsync(string documentsDirectory, AdoptionCandidate candidate, string name, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        try
        {
            string? manifest = null;
            if (candidate.Kind == AdoptionCandidateKind.AddIn)
            {
                var uid = candidate.Path[(candidate.Path.LastIndexOf('/') + 1)..];
                manifest = await ManifestAsync(documentsDirectory, uid, cancellationToken);
                if (manifest is null)
                {
                    return Failed(DiagnosticCodes.ModAdoptSourceMissing, $"AddIns.xml no longer registers {uid}, so it cannot be adopted as an add-in.");
                }
            }

            using var source = await CandidateModSource.CreateAsync(_fileSystem, documentsDirectory, candidate, name, manifest, cancellationToken);
            var imported = await _importer.ImportAsync(source, new ModOrigin(ModOriginKind.Adopted, candidate.Path), cancellationToken);
            return imported.TryGetValue(out var import, out var error) ? Result.Success(import.Entry) : Result.Failure<LibraryEntry>(error);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException or XmlException)
        {
            return Failed(DiagnosticCodes.ModImportFailed, $"{candidate.Name} could not be imported from {candidate.Path}: {exception.Message}");
        }
    }

    private static Result<LibraryEntry> Failed(string code, string message) => Result.Failure<LibraryEntry>(new Error(code, message));

    /// <summary>A manifest holding the add-in's <c>AddInItem</c> as <c>AddIns.xml</c> has it, or null when it is not registered.</summary>
    private async Task<string?> ManifestAsync(string documentsDirectory, string uid, CancellationToken cancellationToken)
    {
        var path = DocumentsPaths.Resolve(_fileSystem, documentsDirectory, "Settings/AddIns.xml");
        if (!_fileSystem.File.Exists(path))
        {
            return null;
        }

        await using var stream = _fileSystem.File.OpenRead(path);
        var document = await XDocument.LoadAsync(stream, LoadOptions.PreserveWhitespace, cancellationToken);
        var item = document.Descendants("AddInItem").FirstOrDefault(e => string.Equals((string?)e.Attribute("UID"), uid, StringComparison.OrdinalIgnoreCase));
        return item is null
            ? null
            : $"<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<Manifest Type=\"AddIn\">\n<AddInsList>\n{item.ToString(SaveOptions.DisableFormatting)}\n</AddInsList>\n</Manifest>\n";
    }
}
