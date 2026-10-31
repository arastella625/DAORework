using DAORework.Application.Fixes;
using DAORework.Application.Mods.Apply;
using DAORework.Application.Mods.Backups;
using DAORework.Application.Tests.Fakes;
using DAORework.Core.Catalog;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Fixes;
using DAORework.Core.Mods.Planning;
using Xunit;

namespace DAORework.Application.Tests.Fixes;

public sealed class DxvkFixTests
{
    private const string Install = "D:/Games/Dragon Age";
    private const string Release = "C:/Downloads/dxvk-2.3.tar.gz";
    private const string Dll = DxvkFix.DllPath;

    private readonly InMemoryDeploymentStores _stores = new();
    private readonly FakeDeploymentBackupStore _backups = new();
    private readonly FakeFixLibrary _library = new();
    private readonly FakeInstallFiles _files = new();
    private readonly FakeFixSourceReader _sources = new();
    private readonly FakeKnownItemsCatalogSource _catalog = new([TestDxvk.CatalogItem]);

    public DxvkFixTests()
    {
        _sources.Sources[Release] = TestDxvk.Dll("2.3");
    }

    [Fact]
    public async Task Apply_WhereThereIsNoD3d9_AddsTheDllAsVersionedEntry()
    {
        var planned = (await Plan(FixChangeKind.Apply, Release)).Value;

        Assert.Equal((PlanAction.Copy, Dll), (Assert.Single(planned.Plan.Change.Operations).Action, planned.Plan.Change.Operations[0].TargetPath));
        Assert.StartsWith("dxvk-2-3-", planned.Plan.EntryId, StringComparison.Ordinal);
        Assert.Equal("2.3", planned.Prepared!.Entry.Version);
        Assert.Empty(planned.Prepared.AlsoKeep);
    }

    [Fact]
    public async Task Apply_OverDxvkPlacedByHand_KeepsItSoRemovalPutsItBack()
    {
        _files.Files[Dll] = TestDxvk.Dll("2.2");

        var planned = (await Plan(FixChangeKind.Apply, Release)).Value;
        await ApplyFix().ExecuteAsync(new ApplyFixRequest(Install, planned));
        _files.Files[Dll] = TestDxvk.Dll("2.3");
        var removal = (await Plan(FixChangeKind.Remove)).Value;

        Assert.Equal(PlanAction.Replace, Assert.Single(planned.Plan.Change.Operations).Action);
        Assert.Equal("2.2", Assert.Single(planned.Prepared!.AlsoKeep).Entry.Version);
        Assert.Equal(2, _library.Entries.Count);
        var putBack = Assert.Single(removal.Plan.Change.Operations);
        Assert.Equal((PlanAction.Replace, "2.2"), (putBack.Action, _library.Entries.Single(e => e.LibraryId == putBack.Source!.ModId).Version));
    }

    [Fact]
    public async Task Apply_OverAnotherWrapper_IsBlockedAndSaysWhy()
    {
        _files.Files[Dll] = TestDxvk.NotDxvk();

        var planned = (await Plan(FixChangeKind.Apply, Release)).Value;

        Assert.Equal(PlanBlockerKind.UnmanagedFileInTheWay, Assert.Single(planned.Plan.Change.Blockers).Kind);
        Assert.Contains("ReShade", Assert.Single(planned.Notes), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Apply_TheSameDxvkAlreadyThereByHand_ChangesNothing()
    {
        _files.Files[Dll] = TestDxvk.Dll("2.3");

        var planned = (await Plan(FixChangeKind.Apply, Release)).Value;

        Assert.True(planned.Plan.Change.IsEmpty);
        Assert.Empty(planned.Plan.Change.NextRecord.ActiveModIds);
    }

    [Fact]
    public async Task ApplyThenRemove_DeletesWhatItAdded()
    {
        await ApplyFix().ExecuteAsync(new ApplyFixRequest(Install, (await Plan(FixChangeKind.Apply, Release)).Value));
        _files.Files[Dll] = TestDxvk.Dll("2.3");

        var removal = (await Plan(FixChangeKind.Remove)).Value;

        Assert.Equal(PlanAction.Delete, Assert.Single(removal.Plan.Change.Operations).Action);
    }

    [Fact]
    public async Task Apply_WithoutARelease_SaysWhereToGetOne()
    {
        var result = await Plan(FixChangeKind.Apply, source: null);

        Assert.Equal(DiagnosticCodes.FixSourceRequired, result.Error!.Code);
        Assert.Contains("github.com/doitsujin/dxvk/releases", result.Error.Recommendation, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Apply_The64BitBuild_IsRefused()
    {
        _sources.Sources[Release] = TestDxvk.Dll("2.3", machine: 0x8664);

        Assert.Equal(DiagnosticCodes.FixSourceInvalid, (await Plan(FixChangeKind.Apply, Release)).Error!.Code);
    }

    [Fact]
    public async Task Apply_CatalogWithoutDxvk_Fails()
    {
        var withoutDxvk = new FakeKnownItemsCatalogSource([TestDxvk.CatalogItem with { Id = "other" }]);

        var result = await new PlanFix(_stores, _stores, _library, _files, [new DxvkFix(_files, _sources, withoutDxvk)]).ExecuteAsync(new PlanFixRequest(Install, "dxvk", FixChangeKind.Apply, Release));

        Assert.Equal(DiagnosticCodes.CatalogInvalid, result.Error!.Code);
    }

    [Fact]
    public async Task Apply_CatalogThatCannotLoad_Fails()
    {
        var broken = new FakeKnownItemsCatalogSource(Result.Failure<KnownItemsCatalog>(new Error(DiagnosticCodes.CatalogInvalid, "broken")));

        var result = await new PlanFix(_stores, _stores, _library, _files, [new DxvkFix(_files, _sources, broken)]).ExecuteAsync(new PlanFixRequest(Install, "dxvk", FixChangeKind.Apply, Release));

        Assert.Equal("broken", result.Error!.Message);
    }

    private Task<Result<PlannedFix>> Plan(FixChangeKind kind, string? source = null) =>
        new PlanFix(_stores, _stores, _library, _files, [new DxvkFix(_files, _sources, _catalog)]).ExecuteAsync(new PlanFixRequest(Install, "dxvk", kind, source));

    private ApplyFix ApplyFix() =>
        new(_library, new ApplyModPlan(new BackUpDeployment(_backups), _stores, new JournaledApplyRunner(_stores, _stores, _stores)), _stores);
}
