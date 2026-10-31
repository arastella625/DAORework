using DAORework.Core.Mods.Library;

namespace DAORework.Core.Tests.Mods;

/// <summary>Library mods for tests. A file's content hash is its path unless given as "path#hash".</summary>
internal static class Mods
{
    public static LibraryMod Override(string id, params string[] files) => new(id, id, ModKind.Override, Files(files));

    public static LibraryMod AddIn(string id, string uid, params string[] files) => new(id, id, ModKind.AddIn, Files(files), uid);

    private static List<LibraryModFile> Files(string[] files) =>
        files.Select(f => f.Split('#') is [var path, var hash] ? new LibraryModFile(path, 10, hash) : new LibraryModFile(f, 10, f)).ToList();
}
