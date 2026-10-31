namespace DAORework.Core.Mods.Library;

/// <summary>What a library file is (ADR-0018), recorded when the mod is imported.</summary>
public enum ModFileKind
{
    /// <summary>Not classified yet: the entry was written before <c>mod.json</c> schema 3 and is classified when next read.</summary>
    Unclassified,

    /// <summary>Game data, deployed like any mod file.</summary>
    Data,

    /// <summary>A program: by its extension or a Windows executable header. Never deployed.</summary>
    Program,
}
