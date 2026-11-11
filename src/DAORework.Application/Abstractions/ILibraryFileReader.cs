using DAORework.Core.Common;
using DAORework.Core.Mods.Library;

namespace DAORework.Application.Abstractions;

/// <summary>
/// Reads one file of a mod in DAORework's library as it is now (ADR-0033 §2): where it is, its SHA-256, and its first
/// bytes, so a program can be checked and scanned again before it goes into the game folder. Reads only.
/// </summary>
public interface ILibraryFileReader
{
    Task<Result<LibraryFileRead>> ReadAsync(string modId, LibraryModFile file, int startBytes, CancellationToken cancellationToken = default);
}
