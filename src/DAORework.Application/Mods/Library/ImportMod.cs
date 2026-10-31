using DAORework.Application.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Library;

namespace DAORework.Application.Mods.Library;

/// <summary>Adds a downloaded mod to DAORework's library (ROADMAP P2-07). Only the library folder is written.</summary>
public sealed class ImportMod
{
    private readonly IModLibrary _library;

    public ImportMod(IModLibrary library)
    {
        _library = library;
    }

    public Task<Result<LibraryImport>> ExecuteAsync(ImportModRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return string.IsNullOrWhiteSpace(request.SourcePath)
            ? Task.FromResult(Result.Failure<LibraryImport>(new Error(DiagnosticCodes.ModImportFailed, "Give the path of a .dazip, a .zip or an extracted mod folder.")))
            : _library.ImportAsync(request.SourcePath, cancellationToken);
    }
}
