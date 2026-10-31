using DAORework.Core.Mods.Library;

namespace DAORework.Core.Fixes;

/// <summary>One file a fix places: where it goes, relative to the install folder with <c>/</c> separators, and its library copy.</summary>
public sealed record FixFile(string TargetPath, LibraryModFile Applied);
