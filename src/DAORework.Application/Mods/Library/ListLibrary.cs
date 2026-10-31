using DAORework.Application.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Mods.Library;

namespace DAORework.Application.Mods.Library;

/// <summary>The mods in DAORework's library, sorted by name.</summary>
public sealed class ListLibrary
{
    private readonly IModLibrary _library;

    public ListLibrary(IModLibrary library)
    {
        _library = library;
    }

    public async Task<Result<IReadOnlyList<LibraryEntry>>> ExecuteAsync(CancellationToken cancellationToken = default) =>
        Result.Success(await _library.ListAsync(cancellationToken));
}
