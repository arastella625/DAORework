using System.IO.Abstractions.TestingHelpers;
using DAORework.Application.Installations;
using DAORework.Application.Tests.Fakes;
using DAORework.Core.Common;
using DAORework.Core.Installations;
using Xunit;

namespace DAORework.Application.Tests.Installations;

public sealed class InstallationProbeTests
{
    private readonly MockFileSystem _fileSystem = new();

    [Fact]
    public void Probe_NoExecutables_ReturnsNull()
    {
        _fileSystem.AddDirectory(TestPaths.Root("Games", "DAO"));

        Assert.Null(new InstallationProbe(_fileSystem).Probe(TestPaths.Root("Games", "DAO")));
    }

    [Fact]
    public void Probe_BothExecutables_ReportsBothAndTrimsTheTrailingSeparator()
    {
        var root = TestPaths.Root("Games", "DAO");
        _fileSystem.AddFile(GamePaths.GameExecutable(root), new MockFileData("exe"));
        _fileSystem.AddFile(GamePaths.LauncherExecutable(root), new MockFileData("exe"));

        var installation = new InstallationProbe(_fileSystem).Probe(root + Path.DirectorySeparatorChar, Distribution.Manual, "by hand");

        Assert.NotNull(installation);
        Assert.Equal(root, installation.InstallDirectory);
        Assert.Equal(GamePaths.GameExecutable(root), installation.GameExecutablePath);
        Assert.Equal(GamePaths.LauncherExecutable(root), installation.LauncherExecutablePath);
        Assert.Equal(Distribution.Manual, installation.Distribution);
        Assert.Equal("by hand", installation.Notes);
    }

    [Theory]
    [InlineData(@"C:\Program Files (x86)\Steam\steamapps\common\Dragon Age Ultimate Edition", Distribution.Steam)]
    [InlineData(@"D:\GOG Games\Dragon Age Origins", Distribution.Gog)]
    [InlineData(@"C:\Program Files\EA Games\Dragon Age", Distribution.EaApp)]
    [InlineData(@"C:\Program Files (x86)\Origin Games\Dragon Age", Distribution.EaApp)]
    [InlineData(@"E:\Games\DAO", Distribution.Unknown)]
    public void GuessDistribution_ReadsTheFolderName(string path, Distribution expected)
    {
        Assert.Equal(expected, InstallationProbe.GuessDistribution(path));
    }
}
