using System.IO.Abstractions;
using DAORework.Application.Abstractions;

namespace DAORework.App.Tests.Fakes;

/// <summary>A Recycle Bin over the mock file system: what is sent is noted, then gone from where it was.</summary>
internal sealed class FakeRecycleBin(IFileSystem fileSystem) : IRecycleBin
{
    public bool IsAvailable { get; set; } = true;

    public List<string> Sent { get; } = [];

    public void Send(string path)
    {
        Sent.Add(path);
        if (fileSystem.Directory.Exists(path))
        {
            fileSystem.Directory.Delete(path, recursive: true);
        }
        else
        {
            fileSystem.File.Delete(path);
        }
    }
}
