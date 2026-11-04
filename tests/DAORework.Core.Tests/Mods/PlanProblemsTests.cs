using DAORework.Core.Mods.Library;
using DAORework.Core.Mods.Planning;
using Xunit;

namespace DAORework.Core.Tests.Mods;

/// <summary>What stops a plan, one problem per cause however many files it touches, each saying what to do.</summary>
public sealed class PlanProblemsTests
{
    private const string OwnedOverride = "packages/core/override/DAORework/";

    [Fact]
    public void For_AModThatRunsInTheGame_SaysItStaysInTheLibrary()
    {
        var plan = Plan([Program("luma")]);

        var problem = Assert.Single(PlanProblems.For(plan, Name));

        Assert.Equal("Luma runs inside the game, and DAORework can't install that kind of mod yet.", problem.Title);
        Assert.Equal("It stays in your Library, off.", problem.WhatToDo);
        Assert.Equal(["luma"], Assert.Single(plan.Blockers).LeaveOutModIds);
    }

    [Fact]
    public void For_AnActiveModThatRunsInTheGame_SaysTurnItOff()
    {
        var current = DeploymentRecord.Empty with { ActiveModIds = ["luma"] };

        var problem = Assert.Single(PlanProblems.For(Plan([Program("luma")], current), Name));

        Assert.Equal("Turn it off first.", problem.WhatToDo);
    }

    [Fact]
    public void For_ManyFilesInTheWay_IsOneProblem()
    {
        var files = Enumerable.Range(0, 400).Select(i => $"hair{i}.dds").ToArray();
        var disk = files.ToDictionary(f => $"{OwnedOverride}hair/{f}", _ => "theirs");

        var plan = Plan([Mods.Override("hair", files)], disk: disk);
        var problem = Assert.Single(PlanProblems.For(plan, Name));

        Assert.Equal(400, plan.Blockers.Count);
        Assert.Equal("400 files from Hair are already in your game, and DAORework didn't put them there.", problem.Title);
    }

    [Fact]
    public void For_FileChangedAfterwards_CannotBeLeftOut()
    {
        var current = DeploymentRecord.Empty with { ActiveModIds = ["hair"], Files = [new RecordedFile($"{OwnedOverride}hair/hair.dds", "placed", "hair")] };
        var disk = new Dictionary<string, string> { [$"{OwnedOverride}hair/hair.dds"] = "changed" };

        var plan = Plan([Mods.Override("hair", "hair.dds#new")], current, disk);

        Assert.StartsWith("1 file DAORework placed was changed afterwards", Assert.Single(PlanProblems.For(plan, Name)).Title, StringComparison.Ordinal);
        Assert.Empty(Assert.Single(plan.Blockers).LeaveOutModIds);
    }

    [Fact]
    public void For_ManyCauses_StopsAtFiveRows()
    {
        var pairs = Enumerable.Range(0, 8).SelectMany(i => new[] { AddIn($"a{i}", $"shared{i}"), AddIn($"b{i}", $"shared{i}") }).ToList();

        var problems = PlanProblems.For(Plan(pairs), Name);

        Assert.Equal(5, problems.Count);
        Assert.Equal("4 more problems", problems[^1].Title);
    }

    [Fact]
    public void Blocker_TwoModsWritingOnePath_LeavesOutTheLater()
    {
        var plan = Plan([AddIn("first", "shared"), AddIn("second", "shared")]);

        Assert.Equal(["second"], Assert.Single(plan.Blockers).LeaveOutModIds);
        Assert.StartsWith("first and second would write 1 file to the same place", Assert.Single(PlanProblems.For(plan, id => id)).Title, StringComparison.Ordinal);
    }

    [Fact]
    public void For_DxvkInAZip_SaysWhereDxvkIs()
    {
        var dxvk = new LibraryMod("dxvk", "Dxvk", ModKind.GameProgram, [new LibraryModFile("bin_ship/d3d9.dll", 1, "a") { Kind = ModFileKind.Program }]);

        Assert.EndsWith("For DXVK, use the Fixes page.", Assert.Single(PlanProblems.For(Plan([dxvk]), Name)).WhatToDo, StringComparison.Ordinal);
    }

    [Fact]
    public void Summary_MoreCausesThanRows_CountsEveryCause()
    {
        var pairs = Enumerable.Range(0, 8).SelectMany(i => new[] { AddIn($"a{i}", $"shared{i}"), AddIn($"b{i}", $"shared{i}") }).ToList();

        Assert.Equal("Can't go ahead yet: 8 problems to sort out first.", ModActivationPlanSummary.Line(Plan(pairs), Name));
    }

    [Fact]
    public void Summary_Blocked_CountsTheProblems()
    {
        Assert.Equal("Can't go ahead yet: 1 problem to sort out first.", ModActivationPlanSummary.Line(Plan([Program("luma")]), Name));
    }

    private static string Name(string id) => char.ToUpperInvariant(id[0]) + id[1..];

    private static LibraryMod Program(string id) => new(id, Name(id), ModKind.GameProgram, [new LibraryModFile("bin_ship/dinput8.dll", 1, "a") { Kind = ModFileKind.Program }]);

    private static LibraryMod AddIn(string id, string folder) =>
        new(id, Name(id), ModKind.AddIn, [new LibraryModFile("Manifest.xml", 1, id), new LibraryModFile($"Contents/addins/{folder}/data.erf", 1, id)], "uid-" + id);

    private static ModActivationPlan Plan(IReadOnlyList<LibraryMod> mods, DeploymentRecord? current = null, Dictionary<string, string>? disk = null) =>
        ModActivationPlanner.Plan(new PlanInput(current ?? DeploymentRecord.Empty, mods, [], disk ?? [], [])).Value;
}
