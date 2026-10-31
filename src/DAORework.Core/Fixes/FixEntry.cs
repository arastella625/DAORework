using DAORework.Core.Mods.Library;

namespace DAORework.Core.Fixes;

/// <summary>
/// A fix as held in the library (ADR-0013): the files it places, and <see cref="Kept"/>, copies of files it replaces
/// (the unpatched <c>daorigins.exe</c>), so that removing it can put them back without the backup or the download.
/// </summary>
public sealed record FixEntry(string Id, string Fix, string? Version, IReadOnlyList<FixFile> Files, IReadOnlyList<LibraryModFile> Kept)
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;

    public string LibraryId => FixIds.LibraryId(Id);
}
