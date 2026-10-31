namespace DAORework.Core.Mods.Backups;

/// <summary>
/// One path a plan touches, as it was before. <see cref="BeforeSha256"/> null means nothing was there: restoring
/// removes whatever the plan put there, but only while it still hashes to <see cref="AfterSha256"/> (or always, when
/// that is not known, as for <c>AddIns.xml</c>). Otherwise <see cref="StoredAs"/> names the saved copy to put back.
/// </summary>
public sealed record BackupEntry(string TargetPath, string? BeforeSha256, string? StoredAs, string? AfterSha256)
{
    /// <summary>
    /// Gets where a moved file was before the plan moved it to <see cref="TargetPath"/> (ADR-0015). It is not copied:
    /// restoring moves it back, or copies it from the library by <see cref="AfterSha256"/> when it is gone.
    /// </summary>
    public string? MovedFrom { get; init; }
}
