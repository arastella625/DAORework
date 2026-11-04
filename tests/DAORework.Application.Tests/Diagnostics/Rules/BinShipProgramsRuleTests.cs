using System.IO.Abstractions.TestingHelpers;
using DAORework.Application.Diagnostics.Rules;
using DAORework.Application.Tests.Fakes;
using DAORework.Core.Catalog;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Planning;
using DAORework.Core.Profiles;
using Xunit;

namespace DAORework.Application.Tests.Diagnostics.Rules;

/// <summary>Programs in <c>bin_ship</c> that nobody recorded (P2.8-03, ADR-0033 §6).</summary>
public sealed class BinShipProgramsRuleTests
{
    private static readonly string _install = TestPaths.Root("Game");

    private readonly MockFileSystem _fileSystem = new();
    private readonly InMemoryDeploymentStores _records = new();

    private RuntimeProfile Profile => new() { InstallDirectory = _install };

    [Fact]
    public async Task EvaluateAsync_OnlyTheGamesOwn_ReportsNothing()
    {
        Add("binkw32.dll", "FMODEX.DLL", "daorigins.exe");

        Assert.Empty(await Rule().EvaluateAsync(Profile));
    }

    [Fact]
    public async Task EvaluateAsync_NoBinShipOrNoInstallFolder_ReportsNothing()
    {
        Assert.Empty(await Rule().EvaluateAsync(Profile));
        Assert.Empty(await Rule().EvaluateAsync(new RuntimeProfile()));
    }

    [Fact]
    public async Task EvaluateAsync_AHandPlacedDll_IsNamed()
    {
        Add("binkw32.dll", "dinput8.dll", "dxgi.dll");

        var issue = Assert.Single(await Rule().EvaluateAsync(Profile));

        Assert.Equal(DiagnosticCodes.InstallProgramFound, issue.Code);
        Assert.Equal(DiagnosticSeverity.Info, issue.Severity);
        Assert.Contains("2 program(s) DAORework did not place: dinput8.dll, dxgi.dll.", issue.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EvaluateAsync_AFileInTheRecord_IsLeftOut()
    {
        Add("d3d9.dll");
        await _records.SaveAsync(_install, new DeploymentRecord([], [new RecordedFile("bin_ship/d3d9.dll", "a", "fix-dxvk")], []));

        Assert.Empty(await Rule().EvaluateAsync(Profile));
    }

    [Fact]
    public async Task EvaluateAsync_ACatalogName_NeedsAFingerprintWithoutAMarker()
    {
        Add("dinput8.dll", "d3d9.dll");
        KnownItem[] items =
        [
            new("dao-lib", "DAO Lib", KnownItemKind.Mod, "https://example.com", "x", [new KnownItemFingerprint(FingerprintKind.GameFile, "bin_ship/dinput8.dll")]),
            new("dxvk", "DXVK", KnownItemKind.Fix, "https://example.com", "x", [new KnownItemFingerprint(FingerprintKind.GameFile, "bin_ship/d3d9.dll", "DXVK: ")]),
        ];

        var issue = Assert.Single(await Rule(items).EvaluateAsync(Profile));

        Assert.Contains("d3d9.dll, dinput8.dll (DAO Lib).", issue.Message, StringComparison.Ordinal);
    }

    private BinShipProgramsRule Rule(params KnownItem[] items) => new(_fileSystem, _records, new FakeKnownItemsCatalogSource(items));

    private void Add(params string[] names)
    {
        foreach (var name in names)
        {
            _fileSystem.AddFile(Path.Combine(_install, GamePaths.BinShipDirectoryName, name), new MockFileData("x"));
        }
    }
}
