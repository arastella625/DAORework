namespace DAORework.Core.Mods.Library;

/// <summary>The outcome of an import: the library entry, and whether it was already there (the same download imported before).</summary>
public sealed record LibraryImport(LibraryEntry Entry, bool AlreadyInLibrary);
