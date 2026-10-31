using System.IO.Abstractions.TestingHelpers;
using DAORework.Application.Catalog.Matchers;
using DAORework.Core.Catalog;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Formats;
using DAORework.Core.Profiles;
using Xunit;
using static DAORework.Application.Tests.Catalog.CatalogTestData;

namespace DAORework.Application.Tests.Catalog;

public sealed class GameFileMatcherTests
{
    private static readonly string _relative = Path.Combine("bin_ship", "d3d9.dll");

    private readonly MockFileSystem _fileSystem = new();

    [Fact]
    public async Task MatchAsync_NoFile_MatchesNothing()
    {
        Assert.Empty((await Match(Dxvk.Fingerprints[0])).Value);
    }

    [Fact]
    public async Task MatchAsync_NoInstallDirectory_MatchesNothing()
    {
        _fileSystem.AddFile(D3D9, new MockFileData(DxvkDll("v2.3")));

        var result = await new GameFileMatcher(_fileSystem).MatchAsync(Dxvk.Fingerprints[0], new RuntimeProfile());

        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task MatchAsync_Dxvk32Bit_MatchesWithTheVersion()
    {
        _fileSystem.AddFile(D3D9, new MockFileData(DxvkDll("v2.3")));

        var match = Assert.Single((await Match(Dxvk.Fingerprints[0])).Value);

        Assert.Equal(_relative, match.Location);
        Assert.Equal("v2.3", match.Version);
        Assert.Null(match.WrongArchitecture);
    }

    [Fact]
    public async Task MatchAsync_Dxvk64Bit_MatchesAndFlagsTheArchitecture()
    {
        _fileSystem.AddFile(D3D9, new MockFileData(DxvkDll("v3.1.1", PortableExecutableMachine.X64)));

        var match = Assert.Single((await Match(Dxvk.Fingerprints[0])).Value);

        Assert.Equal("v3.1.1", match.Version);
        Assert.Contains("64-bit (x64)", match.WrongArchitecture, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MatchAsync_SameNameWithoutTheMarker_IsUnrecognized()
    {
        _fileSystem.AddFile(D3D9, new MockFileData(Pe(PortableExecutableMachine.X86, 0x2102)));

        var result = await Match(Dxvk.Fingerprints[0]);

        Assert.Equal(DiagnosticCodes.KnownItemUnrecognized, result.Error!.Code);
        Assert.Contains(_relative, result.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MatchAsync_FileTooLargeToSearch_IsUnrecognizedWithoutReadingIt()
    {
        _fileSystem.AddFile(D3D9, new MockFileData(new byte[GameFileMatcher.MaxMarkerFileBytes + 1]));

        var result = await Match(Dxvk.Fingerprints[0]);

        Assert.Equal(DiagnosticCodes.KnownItemUnrecognized, result.Error!.Code);
    }

    [Fact]
    public async Task MatchAsync_WithoutAMarker_MatchesOnPresenceAndStillChecksTheArchitecture()
    {
        _fileSystem.AddFile(D3D9, new MockFileData(Pe(PortableExecutableMachine.X64, 0x2022)));

        var match = Assert.Single((await Match(new KnownItemFingerprint(FingerprintKind.GameFile, @"bin_ship\d3d9.dll"))).Value);

        Assert.Null(match.Version);
        Assert.NotNull(match.WrongArchitecture);
    }

    [Fact]
    public async Task MatchAsync_AFileThatIsNotABinary_HasNoArchitectureProblem()
    {
        _fileSystem.AddFile(Path.Combine(Install, "bin_ship", "dxvk.conf"), new MockFileData("dxvk.hud = 0"));

        var match = Assert.Single((await Match(new KnownItemFingerprint(FingerprintKind.GameFile, "bin_ship/dxvk.conf"))).Value);

        Assert.Null(match.WrongArchitecture);
    }

    [Fact]
    public async Task MatchAsync_LockedFile_IsUnreadable()
    {
        _fileSystem.AddFile(D3D9, new MockFileData(DxvkDll("v2.3")) { AllowedFileShare = FileShare.None });

        var result = await Match(Dxvk.Fingerprints[0]);

        Assert.Equal(DiagnosticCodes.KnownItemUnreadable, result.Error!.Code);
    }

    private Task<Result<IReadOnlyList<FingerprintMatch>>> Match(KnownItemFingerprint fingerprint) =>
        new GameFileMatcher(_fileSystem).MatchAsync(fingerprint, Profile);
}
