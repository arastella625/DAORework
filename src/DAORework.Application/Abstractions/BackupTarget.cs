namespace DAORework.Application.Abstractions;

/// <summary>
/// A path to capture before a plan runs, relative to the documents folder with <c>/</c> separators. With
/// <see cref="ExpectedSha256"/> the file must be there with that hash; without it, and without
/// <see cref="AnyContent"/>, nothing may be there. <see cref="AnyContent"/> takes whatever is there, or its absence
/// (<c>AddIns.xml</c>). <see cref="AfterSha256"/> is what the plan writes, so a restore removes only that.
/// </summary>
public sealed record BackupTarget(string TargetPath, string? ExpectedSha256, string? AfterSha256, bool AnyContent = false)
{
    /// <summary>
    /// Gets where the plan moves the file at <see cref="TargetPath"/> from. The file must be there with
    /// <see cref="AfterSha256"/> and nothing at the target; only the move is recorded, nothing is copied (ADR-0015).
    /// </summary>
    public string? MovedFrom { get; init; }
}
