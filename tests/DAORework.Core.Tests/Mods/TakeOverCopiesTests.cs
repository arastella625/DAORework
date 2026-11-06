using DAORework.Core.Mods.Library;
using DAORework.Core.Mods.Planning;
using Xunit;

namespace DAORework.Core.Tests.Mods;

/// <summary>Turning a mod on takes over identical files already in the game (ADR-0037).</summary>
public sealed class TakeOverCopiesTests
{
    private const string Override = "packages/core/override/";

    private static readonly HashSet<string> _nothing = [];

    [Fact]
    public void Apply_ACopyOfAFileAlreadyThere_BecomesAMoveOfThatFile()
    {
        var plan = TakeOverCopies.Apply(Plan(("Old/hair.dds", "a")), _nothing);

        var move = Assert.Single(plan.Operations);
        Assert.Equal(PlanAction.Move, move.Action);
        Assert.Equal(Override + "Old/hair.dds", move.MovedFrom);
        Assert.Equal("a", move.ExpectedSha256);
        Assert.Null(move.Source);
    }

    [Fact]
    public void Apply_TwoIdenticalCopies_MovesOneAndDeletesTheOther()
    {
        var plan = TakeOverCopies.Apply(Plan(("Old/hair.dds", "a"), ("Older/hair.dds", "a")), _nothing);

        Assert.Equal(1, plan.Count(PlanAction.Move));
        Assert.Equal(Override + "Older/hair.dds", Assert.Single(plan.Operations, o => o.Action == PlanAction.Delete).TargetPath);
    }

    [Fact]
    public void Apply_AFileThatDiffers_IsLeftAloneAndTheModIsCopied()
    {
        var plan = TakeOverCopies.Apply(Plan(("Old/hair.dds", "other")), _nothing);

        Assert.Equal(PlanAction.Copy, Assert.Single(plan.Operations).Action);
    }

    [Fact]
    public void Apply_WhatVortexWouldPutBack_IsLeftAloneAndTheModIsCopied()
    {
        var plan = TakeOverCopies.Apply(Plan(("Old/hair.dds", "a")), new HashSet<string>(StringComparer.OrdinalIgnoreCase) { Override + "OLD/hair.dds" });

        Assert.Equal(PlanAction.Copy, Assert.Single(plan.Operations).Action);
    }

    [Fact]
    public void Apply_AModAlreadyOn_TakesNothingOver()
    {
        var mod = Mod();
        var recorded = new RecordedFile(Override + "DAORework/hair-1/hair.dds", "a", "hair-1");
        var hashes = new Dictionary<string, string> { [recorded.TargetPath] = "a", [Override + "Old/hair.dds"] = "a" };
        var input = new PlanInput(new DeploymentRecord(["hair-1"], [recorded], []), [mod], ["Old/hair.dds"], hashes, []);

        Assert.Empty(TakeOverCopies.Apply(ModActivationPlanner.Plan(input).Value, _nothing).Operations);
    }

    private static ModActivationPlan Plan(params (string Path, string Sha)[] loose)
    {
        var hashes = loose.ToDictionary(l => Override + l.Path, l => l.Sha);
        return ModActivationPlanner.Plan(new PlanInput(DeploymentRecord.Empty, [Mod()], [.. loose.Select(l => l.Path)], hashes, [])).Value;
    }

    private static LibraryMod Mod() => new("hair-1", "Hair", ModKind.Override, [new LibraryModFile("hair.dds", 10, "a")]);
}
