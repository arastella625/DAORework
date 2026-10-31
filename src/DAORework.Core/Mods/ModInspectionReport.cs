namespace DAORework.Core.Mods;

public sealed class ModInspectionReport
{
    public required DateTimeOffset CreatedAtUtc { get; init; }

    public required string ProfileName { get; init; }

    public required string UserDocumentsDirectory { get; init; }

    public required ModFolderSummary OverrideFolder { get; init; }

    public required ModFolderSummary AddInsFolder { get; init; }
}
