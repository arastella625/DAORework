using DAORework.Core.Mods.Combining;
using DAORework.Core.Mods.Library;
using DAORework.Core.Mods.Planning;
using Xunit;

namespace DAORework.Core.Tests.Mods.Combining;

/// <summary>The planner deploying a combined chargenmorphcfg.xml in place of the mods' copies (ADR-0025).</summary>
public sealed class CombinedPlanTests
{
    private static readonly LibraryModFile _stored = new("abc123/chargenmorphcfg.xml", 300, "abc123");

    [Fact]
    public void CombinedFiles_FindsEachModsCopy_InLoadOrder_AndNothingElse()
    {
        var copies = CombinedFiles.ChargenMorphCopies([Mods.Override("long", "ChargenMorphCfg.xml", "hair.mmh"), Mods.Override("short", "sub/chargenmorphcfg.xml")]).Value;

        Assert.Equal(["long", "short"], copies.Select(c => c.ModId));
        Assert.All(copies, c => Assert.True(c.IsOverride));
    }

    [Fact]
    public void Plan_WithACombinedFile_DeploysIt_InsteadOfTheCopies_WithNoConflict()
    {
        var plan = ModActivationPlanner.Plan(Input(DeploymentRecord.Empty)).Value;

        Assert.Equal([CombinedFiles.TargetPath("chargenmorphcfg.xml"), "packages/core/override/DAORework/long/hair.mmh"], plan.Operations.Select(o => o.TargetPath).Order(StringComparer.Ordinal));
        Assert.Empty(plan.Conflicts.OverrideConflicts);
        Assert.Contains(plan.NextRecord.Files, f => f.ModId == CombinedFiles.ModId && f.Sha256 == "abc123");
        Assert.DoesNotContain(CombinedFiles.ModId, plan.TargetModIds);
        Assert.Contains("  Combines chargenmorphcfg.xml from long, short (12 entries)", ModActivationPlanText.Render(plan));
        Assert.Contains("  combined files: copy 1", ModActivationPlanText.Render(plan));
        Assert.EndsWith(" Combines chargenmorphcfg.xml from long and short: 12 entries.", ModActivationPlanSummary.Line(plan), StringComparison.Ordinal);
    }

    [Fact]
    public void Plan_WithoutTheCombinedFileAnyMore_RemovesIt()
    {
        var target = CombinedFiles.TargetPath("chargenmorphcfg.xml");
        var current = new DeploymentRecord(["long"], [new RecordedFile(target, "abc123", CombinedFiles.ModId)], []);
        var disk = new Dictionary<string, string> { [target] = "abc123" };

        var plan = ModActivationPlanner.Plan(new PlanInput(current, [Mods.Override("long", "chargenmorphcfg.xml")], [], disk, [])).Value;

        Assert.Contains(plan.Operations, o => o.Action == PlanAction.Delete && o.TargetPath == target);
        Assert.Contains(plan.Operations, o => o.Action == PlanAction.Copy && o.TargetPath == "packages/core/override/DAORework/long/chargenmorphcfg.xml");
    }

    [Fact]
    public void Plan_AProblemCombining_IsAWarning()
    {
        var input = new PlanInput(DeploymentRecord.Empty, [Mods.Override("long", "chargenmorphcfg.xml")], [], new Dictionary<string, string>(), [])
        {
            CombineProblems = ["chargenmorphcfg.xml from Broken cannot be read."],
        };

        var plan = ModActivationPlanner.Plan(input).Value;

        Assert.Contains("chargenmorphcfg.xml from Broken cannot be read.", ModActivationPlanWarnings.For(plan, id => id));
    }

    private static PlanInput Input(DeploymentRecord current) =>
        new(current, [Mods.Override("long", "chargenmorphcfg.xml", "hair.mmh"), Mods.Override("short", "chargenmorphcfg.xml")], [], new Dictionary<string, string>(), [])
        {
            Combined = [new CombinedOverride(CombinedFiles.Deployed(_stored), ["long", "short"], 12)],
        };
}
