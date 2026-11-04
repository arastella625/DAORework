using System.IO.Abstractions.TestingHelpers;
using DAORework.Application.Abstractions;
using DAORework.Application.Diagnostics.Rules;
using DAORework.Application.Tests.Fakes;
using DAORework.Core.Diagnostics;
using DAORework.Core.Profiles;
using Xunit;

namespace DAORework.Application.Tests.Diagnostics.Rules;

/// <summary>The prefix, on Linux (ADR-0031).</summary>
public sealed class ProtonPrefixRuleTests
{
    private const string Install = "/home/me/.local/share/Steam/steamapps/common/Dragon Age Ultimate Edition";
    private const string Documents = "/home/me/.local/share/Steam/steamapps/compatdata/47810/pfx/drive_c/users/steamuser/Documents/BioWare/Dragon Age";

    private readonly MockFileSystem _fileSystem = new();
    private readonly FakeProtonPrefixLocator _prefixes = new();

    [Fact]
    public async Task EvaluateAsync_NotOnLinux_SaysNothing()
    {
        Assert.Empty(await Rule(linux: false).EvaluateAsync(new RuntimeProfile { InstallDirectory = Install }));
    }

    [Fact]
    public async Task EvaluateAsync_ASteamCopyWithNoPrefix_SaysToStartTheGameOnce()
    {
        var issue = Assert.Single(await Rule().EvaluateAsync(new RuntimeProfile { InstallDirectory = Install }));

        Assert.Equal(DiagnosticCodes.ProtonPrefixMissing, issue.Code);
        Assert.Contains("Start the game once from Steam", issue.Recommendation, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EvaluateAsync_ASteamCopyWithAPrefix_SaysNothing()
    {
        _prefixes.Prefixes.Add("/somewhere/pfx");

        Assert.Empty(await Rule().EvaluateAsync(new RuntimeProfile { InstallDirectory = Install }));
    }

    [Fact]
    public async Task EvaluateAsync_AnotherCopy_SaysNothing()
    {
        Assert.Empty(await Rule().EvaluateAsync(new RuntimeProfile { InstallDirectory = "/home/me/Games/dao" }));
    }

    [Fact]
    public async Task EvaluateAsync_AProfileWhoseFolderWasInAPrefixThatHasGone_SaysTheModsWentToo()
    {
        var issue = Assert.Single(await Rule().EvaluateAsync(new RuntimeProfile { InstallDirectory = Install, UserDocumentsDirectory = Documents }));

        Assert.Contains("has gone", issue.Message, StringComparison.Ordinal);
    }

    private ProtonPrefixRule Rule(bool linux = true) => new(_fileSystem, new Host(linux), _prefixes);

    private sealed class Host(bool linux) : IHostPlatform
    {
        public bool IsLinux => linux;
    }
}
