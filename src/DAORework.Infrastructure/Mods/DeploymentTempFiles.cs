namespace DAORework.Infrastructure.Mods;

/// <summary>
/// The temporary file a step writes next to its target before moving it into place. A process that dies mid-step
/// leaves one behind; the next run of the step and a restore both remove it, so neither end state keeps it.
/// </summary>
internal static class DeploymentTempFiles
{
    public const string Suffix = ".daorework-apply";

    public static void RemoveStale(System.IO.Abstractions.IFileSystem fileSystem, string target)
    {
        if (fileSystem.File.Exists(target + Suffix))
        {
            fileSystem.File.Delete(target + Suffix);
        }
    }
}
