using System.IO.Abstractions.TestingHelpers;
using DAORework.Infrastructure.Launching;
using Xunit;

namespace DAORework.Infrastructure.Tests.Launching;

/// <summary>ADR-0022: a Steam install is a folder in <c>steamapps/common</c> that the library's app manifest names.</summary>
public sealed class SteamGameInstallTests
{
    private static readonly string _library = MockUnixSupport.Path(@"D:\SteamLibrary");
    private static readonly string _install = Path.Combine(_library, "steamapps", "common", "Dragon Age Ultimate Edition");
    private static readonly string _manifest = Path.Combine(_library, "steamapps", "appmanifest_47810.acf");

    private readonly MockFileSystem _fileSystem = new();

    [Fact]
    public void FolderNamedByTheAppManifest_IsASteamInstall()
    {
        _fileSystem.AddDirectory(_install);
        _fileSystem.AddFile(_manifest, new MockFileData("\"AppState\" { \"appid\" \"47810\" \"installdir\" \"Dragon Age Ultimate Edition\" }"));

        Assert.True(new SteamGameInstall(_fileSystem).IsSteamInstall(_install));
        Assert.True(new SteamGameInstall(_fileSystem).IsSteamInstall(_install + Path.DirectorySeparatorChar));
    }

    [Fact]
    public void ManifestNamingAnotherFolder_IsNot()
    {
        _fileSystem.AddDirectory(_install);
        _fileSystem.AddFile(_manifest, new MockFileData("\"AppState\" { \"appid\" \"47810\" \"installdir\" \"Dragon Age Origins\" }"));

        Assert.False(new SteamGameInstall(_fileSystem).IsSteamInstall(_install));
    }

    [Fact]
    public void SteamLayoutWithoutTheManifest_IsNot()
    {
        _fileSystem.AddDirectory(_install);

        Assert.False(new SteamGameInstall(_fileSystem).IsSteamInstall(_install));
    }

    [Fact]
    public void UnreadableManifest_IsNot()
    {
        _fileSystem.AddDirectory(_install);
        _fileSystem.AddFile(_manifest, new MockFileData("\"AppState\" { \"installdir\" "));

        Assert.False(new SteamGameInstall(_fileSystem).IsSteamInstall(_install));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(@"C:\GOG Games\Dragon Age Origins")]
    public void NoInstallOrOtherStore_IsNot(string? installDirectory)
    {
        var path = installDirectory is null or "" ? installDirectory : MockUnixSupport.Path(installDirectory);

        Assert.False(new SteamGameInstall(_fileSystem).IsSteamInstall(path));
    }
}
