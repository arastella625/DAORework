using DAORework.Core.Mods.Planning;
using Xunit;

namespace DAORework.Core.Tests.Mods;

/// <summary>The window's plan in one sentence, and mods named by their names (P2.5-18).</summary>
public sealed class ModActivationPlanSummaryTests
{
    private static readonly Dictionary<string, string> _names = new(StringComparer.OrdinalIgnoreCase)
    {
        ["old-ui"] = "Old UI",
        ["hair-a"] = "Hair A",
        ["hair-b"] = "Hair B",
        ["fixpack"] = "Qwinn's Fixpack",
    };

    [Fact]
    public void Line_ASwitch_SaysWhatHappensToTheFilesAndAddIns()
    {
        Assert.Equal("Copies 3 files, deletes 1 and adds 1 add-in to AddIns.xml. Writes 30 B.", ModActivationPlanSummary.Line(Switch()));
    }

    [Fact]
    public void Line_NothingToDo_SaysSo()
    {
        var plan = ModActivationPlanner.Plan(new PlanInput(DeploymentRecord.Empty, [], [], new Dictionary<string, string>(), [])).Value;

        Assert.Equal("Nothing to change: the folder already matches.", ModActivationPlanSummary.Line(plan));
    }

    [Fact]
    public void Line_Blocked_SaysNothingChanges()
    {
        var disk = new Dictionary<string, string> { ["packages/core/override/DAORework/hair/hair.dds"] = "theirs" };
        var plan = ModActivationPlanner.Plan(new PlanInput(DeploymentRecord.Empty, [Mods.Override("hair", "hair.dds")], [], disk, [])).Value;

        Assert.Equal("Blocked: nothing changes until the problems below are resolved.", ModActivationPlanSummary.Line(plan));
    }

    [Fact]
    public void Render_WithNames_NamesEveryModByItsName()
    {
        var lines = ModActivationPlanText.Render(Switch(), nameOf: id => _names[id]);

        Assert.Equal("Mods: 1 (Old UI) -> 3 (Hair A, Hair B, Qwinn's Fixpack)", lines[0]);
        Assert.Contains("  AddIns.xml: add qwinn_fixpack_3 (Qwinn's Fixpack)", lines);
        Assert.Contains("  Old UI: delete 1", lines);
        Assert.Contains("  ! hair.dds is in Hair A and Hair B; Hair B's copy is used. It is a texture or model, so only that mod's look shows.", lines);
        Assert.DoesNotContain(lines, l => l.Contains("hair-a", StringComparison.Ordinal));
    }

    [Fact]
    public void Warnings_WithNames_AreTheTextsWarningsWithoutTheirMarks()
    {
        var warnings = ModActivationPlanWarnings.For(Switch(), id => _names[id]);

        Assert.Equal(2, warnings.Count);
        Assert.Equal("hair.dds is in Hair A and Hair B; Hair B's copy is used. It is a texture or model, so only that mod's look shows.", warnings[0]);
    }

    private static ModActivationPlan Switch()
    {
        var disk = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["packages/core/override/DAORework/old-ui/ui.gda"] = "u1",
        };
        return ModActivationPlanner.Plan(new PlanInput(
            new DeploymentRecord(["old-ui"], [new RecordedFile("packages/core/override/DAORework/old-ui/ui.gda", "u1", "old-ui")], []),
            [
                Mods.Override("hair-a", "hair.dds#1", "eyes.dds#2"),
                Mods.Override("hair-b", "textures/hair.dds#3"),
                Mods.AddIn("fixpack", "qwinn_fixpack_3", "Manifest.xml", "Contents/addins/qwinn_fixpack_3/core/fix.ncs#4"),
            ],
            [@"My Mods\eyes.dds"],
            disk,
            [])).Value;
    }
}
