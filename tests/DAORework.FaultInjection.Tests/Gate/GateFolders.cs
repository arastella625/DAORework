namespace DAORework.FaultInjection.Tests.Gate;

/// <summary>One run's own real folders in the temp directory: DAORework's app-data, a documents folder, an install folder and downloads.</summary>
internal sealed class GateFolders : IDisposable
{
    public GateFolders()
    {
        Root = Path.Combine(Path.GetTempPath(), "DAORework.Gate", Guid.NewGuid().ToString("N"));
        foreach (var folder in new[] { AppData, Documents, Install, Downloads })
        {
            Directory.CreateDirectory(folder);
        }
    }

    public string Root { get; }

    public string AppData => Path.Combine(Root, "app");

    public string Documents => Path.Combine(Root, "Dragon Age");

    public string Install => Path.Combine(Root, "install");

    public string Downloads => Path.Combine(Root, "downloads");

    public void Dispose()
    {
        try
        {
            Directory.Delete(Root, recursive: true);
        }
        catch (IOException)
        {
            // A folder the OS still holds open is left for the temp directory's own clean-up.
        }
    }
}
