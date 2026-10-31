using System.IO.Abstractions.TestingHelpers;
using DAORework.Application.Catalog.Matchers;
using DAORework.Core.Diagnostics;
using DAORework.Core.Formats;
using DAORework.Core.Profiles;
using Xunit;
using static DAORework.Application.Tests.Catalog.CatalogTestData;

namespace DAORework.Application.Tests.Catalog;

public sealed class LargeAddressAwareMatcherTests
{
    private readonly MockFileSystem _fileSystem = new();

    [Fact]
    public async Task MatchAsync_FlagSet_Matches()
    {
        _fileSystem.AddFile(GameExecutable, new MockFileData(Pe(PortableExecutableMachine.X86, 0x0122)));

        var match = Assert.Single((await Match(Profile)).Value);

        Assert.Equal("large-address-aware flag in daorigins.exe", match.Location);
    }

    [Fact]
    public async Task MatchAsync_FlagNotSet_MatchesNothing()
    {
        _fileSystem.AddFile(GameExecutable, new MockFileData(Pe(PortableExecutableMachine.X86, 0x0102)));

        Assert.Empty((await Match(Profile)).Value);
    }

    [Fact]
    public async Task MatchAsync_NoExecutable_MatchesNothing()
    {
        Assert.Empty((await Match(Profile)).Value);
        Assert.Empty((await Match(new RuntimeProfile())).Value);
    }

    [Fact]
    public async Task MatchAsync_NotAnExecutable_IsUnreadable()
    {
        _fileSystem.AddFile(GameExecutable, new MockFileData("text"));

        Assert.Equal(DiagnosticCodes.KnownItemUnreadable, (await Match(Profile)).Error!.Code);
    }

    [Fact]
    public async Task MatchAsync_LockedExecutable_IsUnreadable()
    {
        _fileSystem.AddFile(GameExecutable, new MockFileData(Pe(PortableExecutableMachine.X86, 0x0122)) { AllowedFileShare = FileShare.None });

        Assert.Equal(DiagnosticCodes.KnownItemUnreadable, (await Match(Profile)).Error!.Code);
    }

    private Task<Core.Common.Result<IReadOnlyList<Core.Catalog.FingerprintMatch>>> Match(RuntimeProfile profile) =>
        new LargeAddressAwareMatcher(_fileSystem).MatchAsync(LargeAddressAware.Fingerprints[0], profile);
}
