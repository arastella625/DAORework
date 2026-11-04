namespace DAORework.Core.Mods.Downloads;

/// <summary>
/// <c>new-downloads.json</c> (ADR-0034 §9): whether DAORework looks, and where, when it was turned on, and the files
/// dealt with. Its own file, so <c>preferences.json</c> stays free of file names. <see cref="Asked"/> is the one question
/// on Home.
/// </summary>
public sealed record NewDownloadsSettings(bool Enabled, string? Folder, DateTimeOffset? EnabledAtUtc, IReadOnlyList<HandledDownload> Handled)
{
    public const int CurrentSchemaVersion = 1;

    public static NewDownloadsSettings Off { get; } = new(false, null, null, []);

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;

    public bool Asked { get; init; }
}
