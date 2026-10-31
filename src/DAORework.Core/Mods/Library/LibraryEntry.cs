using DAORework.Core.Mods.Safety;

namespace DAORework.Core.Mods.Library;

/// <summary>A mod in the library with where it came from: its <c>mod.json</c> (ADR-0012).</summary>
public sealed record LibraryEntry(LibraryMod Mod, string SourceName, string SourceSha256, DateTimeOffset ImportedAtUtc)
{
    /// <summary>Version 3 adds each file's <see cref="LibraryModFile.Kind"/> and the mod's <see cref="Scan"/> (ADR-0018).</summary>
    public const int CurrentSchemaVersion = 3;

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;

    /// <summary>Gets whether the mod was imported from a download or adopted from the documents folder (schema 2, ADR-0015).</summary>
    public ModOrigin Origin { get; init; } = ModOrigin.Download;

    /// <summary>Gets the antivirus verdict the mod was imported with (schema 3, ADR-0018).</summary>
    public MalwareScan Scan { get; init; } = MalwareScan.BeforeScanning;

    /// <summary>Gets the total size of the mod's files.</summary>
    public long SizeBytes => Mod.Files.Sum(f => f.SizeBytes);
}
