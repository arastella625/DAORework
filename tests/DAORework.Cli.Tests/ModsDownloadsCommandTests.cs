using System.IO.Abstractions.TestingHelpers;
using System.IO.Compression;
using Xunit;

namespace DAORework.Cli.Tests;

/// <summary><c>mods downloads</c> (ADR-0034 §8): asking is consent, and it only reads.</summary>
public sealed class ModsDownloadsCommandTests : IDisposable
{
    private static readonly string _folder = MockUnixSupport.Path(@"E:\Get");

    private readonly CliTestHost _host = new();

    public void Dispose() => _host.Dispose();

    [Fact]
    public void Downloads_ListsWhatLooksLikeADragonAgeMod_WithTheSettingOff()
    {
        _host.FileSystem.AddFile(Path.Combine(_folder, "Blue Hair-1-1-0.zip"), new MockFileData(Zip("hair.erf")));
        _host.FileSystem.AddFile(Path.Combine(_folder, "Textures.zip"), new MockFileData(Zip("other.dds")));
        _host.FileSystem.AddFile(Path.Combine(_folder, "Dog.dazip"), new MockFileData("anything"));

        var result = _host.Run("mods", "downloads", "--folder", _folder);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Blue Hair-1-1-0.zip", result.Output, StringComparison.Ordinal);
        Assert.Contains("Dog.dazip", result.Output, StringComparison.Ordinal);
        Assert.DoesNotContain("Textures.zip", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void Downloads_AFolderThatIsNotThere_SaysSo()
    {
        var result = _host.Run("mods", "downloads", "--folder", _folder);

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("isn't there", result.Output, StringComparison.Ordinal);
    }

    private static byte[] Zip(string entry)
    {
        using var buffer = new MemoryStream();
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            using var writer = new StreamWriter(zip.CreateEntry(entry).Open());
            writer.Write("x");
        }

        return buffer.ToArray();
    }
}
