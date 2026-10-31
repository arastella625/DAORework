using DAORework.Core.Mods.Library;

namespace DAORework.Core.Mods.Adoption;

/// <summary>
/// A hand-installed mod being adopted (ADR-0015): the library mod it is, imported from disk or matched, and where each
/// of that mod's files is in the documents folder now, by its library path, relative to the documents folder with
/// <c>/</c>. A file at its <see cref="ModLayout"/> target is only recorded; any other is moved there.
/// </summary>
public sealed record AdoptedMod(LibraryMod Mod, IReadOnlyDictionary<string, string> SourceByLibraryPath);
