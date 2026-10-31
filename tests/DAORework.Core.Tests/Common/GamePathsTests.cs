using DAORework.Core.Common;
using Xunit;

namespace DAORework.Core.Tests.Common;

public sealed class GamePathsTests
{
    private static readonly string _install = Path.Combine("C:", "Games", "DAO");
    private static readonly string _documents = Path.Combine("C:", "Users", "Me", "Documents", "BioWare", "Dragon Age");

    [Fact]
    public void GameExecutable_IsUnderBinShip()
    {
        Assert.Equal(Path.Combine(_install, "bin_ship", "daorigins.exe"), GamePaths.GameExecutable(_install));
    }

    [Fact]
    public void LauncherExecutable_IsInTheInstallRoot()
    {
        Assert.Equal(Path.Combine(_install, "DAOriginsLauncher.exe"), GamePaths.LauncherExecutable(_install));
    }

    [Fact]
    public void DefaultDocumentsDirectory_IsBioWareDragonAgeUnderMyDocuments()
    {
        var myDocuments = Path.Combine("C:", "Users", "Me", "Documents");

        Assert.Equal(_documents, GamePaths.DefaultDocumentsDirectory(myDocuments));
    }

    [Fact]
    public void OverrideDirectory_IsPackagesCoreOverride()
    {
        Assert.Equal(Path.Combine(_documents, "packages", "core", "override"), GamePaths.OverrideDirectory(_documents));
    }

    [Fact]
    public void AddInsDirectory_IsAddIns()
    {
        Assert.Equal(Path.Combine(_documents, "AddIns"), GamePaths.AddInsDirectory(_documents));
    }

    [Fact]
    public void KnownInstallRelativePaths_CoverSteamGogAndEa()
    {
        var joined = string.Join("|", GamePaths.KnownInstallRelativePaths);

        Assert.Contains("steamapps", joined, StringComparison.Ordinal);
        Assert.Contains("GOG", joined, StringComparison.Ordinal);
        Assert.Contains("EA Games", joined, StringComparison.Ordinal);
        Assert.Contains("Origin Games", joined, StringComparison.Ordinal);
        Assert.DoesNotContain(GamePaths.KnownInstallRelativePaths, Path.IsPathRooted);
    }
}
