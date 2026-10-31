namespace DAORework.Infrastructure.Tests.Persistence;

/// <summary>A fresh temp directory per test, deleted afterwards (SDD §10).</summary>
public sealed class TempDirectory : IDisposable
{
    public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "DAORework.Tests", Guid.NewGuid().ToString("N"));

    public string File(string relativePath) => System.IO.Path.Combine(Path, relativePath);

    public void Dispose()
    {
        if (Directory.Exists(Path))
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}
