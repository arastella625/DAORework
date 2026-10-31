using DAORework.Core.Mods.Planning;
using Xunit;

namespace DAORework.Core.Tests.Mods;

public sealed class ModActivationPlanTextTests
{
    /// <summary>The whole dry run for a realistic switch, as the CLI and the window will show it.</summary>
    [Fact]
    public void Render_ASwitchWithWarnings_ReadsAsAPlan()
    {
        var disk = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["packages/core/override/DAORework/old-ui/ui.gda"] = "u1",
        };
        var input = new PlanInput(
            new DeploymentRecord(["old-ui"], [new RecordedFile("packages/core/override/DAORework/old-ui/ui.gda", "u1", "old-ui")], []),
            [
                Mods.Override("hair-a", "hair.dds#1", "eyes.dds#2"),
                Mods.Override("hair-b", "textures/hair.dds#3"),
                Mods.AddIn("fixpack", "qwinn_fixpack_3", "Manifest.xml", "Contents/addins/qwinn_fixpack_3/core/fix.ncs#4"),
            ],
            [@"My Mods\eyes.dds"],
            disk,
            []);

        var lines = ModActivationPlanText.Render(ModActivationPlanner.Plan(input).Value);

        Assert.Equal(
            [
                "Mods: 1 (old-ui) -> 3 (hair-a, hair-b, fixpack)",
                "  Copy      3 files",
                "  Replace   0 files",
                "  Delete    1 file",
                "  Unchanged 0 files",
                "  AddIns.xml: add qwinn_fixpack_3 (fixpack)",
                "  Writes 30 B; that much must be free on the documents folder's drive.",
                "By mod:",
                "  fixpack: copy 1",
                "  hair-a: copy 1",
                "  hair-b: copy 1",
                "  old-ui: delete 1",
                "Warnings:",
                "  ! hair.dds is in hair-a and hair-b; hair-b's copy is used.",
                @"  ! eyes.dds from hair-a has the same name as My Mods\eyes.dds in the override folder, which DAORework does not manage; the game picks one.",
            ],
            lines);
    }

    [Fact]
    public void Render_WithFileList_ListsEveryOperationUnderItsMod()
    {
        var plan = ModActivationPlanner.Plan(new PlanInput(DeploymentRecord.Empty, [Mods.Override("hair", "hair.dds")], [], new Dictionary<string, string>(), [])).Value;

        var lines = ModActivationPlanText.Render(plan, listFiles: true);

        Assert.Contains("    + packages/core/override/DAORework/hair/hair.dds", lines);
    }

    [Fact]
    public void Render_NothingToDo_SaysSo()
    {
        var lines = ModActivationPlanText.Render(ModActivationPlanner.Plan(new PlanInput(DeploymentRecord.Empty, [], [], new Dictionary<string, string>(), [])).Value);

        Assert.Equal(["Mods: no mods -> no mods", "Nothing to change: the folder already matches."], lines);
    }

    [Fact]
    public void Render_Blocked_ListsEveryBlockerAndSaysNothingChanges()
    {
        var disk = new Dictionary<string, string> { ["packages/core/override/DAORework/hair/hair.dds"] = "theirs" };
        var plan = ModActivationPlanner.Plan(new PlanInput(DeploymentRecord.Empty, [Mods.Override("hair", "hair.dds")], [], disk, [])).Value;

        var lines = ModActivationPlanText.Render(plan);

        Assert.Contains("No changes can be planned until the problems below are resolved.", lines);
        Assert.Contains("Blocked; nothing will be changed:", lines);
        Assert.Contains(lines, l => l.StartsWith("  x packages/core/override/DAORework/hair/hair.dds is already there", StringComparison.Ordinal));
    }

    [Fact]
    public void Render_IdenticalOverlapAndAddInOverrideAndMissingFiles_AreWarnings()
    {
        var input = new PlanInput(
            new DeploymentRecord(["gone"], [new RecordedFile("packages/core/override/DAORework/gone/g.gda", "g", "gone")], []),
            [Mods.AddIn("dog", "Extra_Dog_Slot", "Contents/addins/Extra_Dog_Slot/dog.gda"), Mods.Override("a", "same.gda#s", "dog.gda"), Mods.Override("b", "same.gda#s")],
            [],
            new Dictionary<string, string>(),
            []);

        var lines = ModActivationPlanText.Render(ModActivationPlanner.Plan(input).Value);

        Assert.Contains("  ! same.gda is in a and b; b's copy is used (identical content).", lines);
        Assert.Contains("  ! dog.gda from a replaces the add-in file AddIns/Extra_Dog_Slot/dog.gda in the game.", lines);
        Assert.Contains("  ! 1 file DAORework placed earlier is missing; any still needed are copied again.", lines);
    }

    [Fact]
    public void Render_MovesForAnAdoption_CountsThemAndListsWhereEachComesFrom()
    {
        var move = new PlanOperation(PlanAction.Move, "packages/core/override/DAORework/mine/a.dds", "mine", null, "h") { MovedFrom = "packages/core/override/My Mods/a.dds" };
        var plan = new ModActivationPlan([], ["mine"], [move], [], 0, [], [], new Core.Mods.Conflicts.ConflictReport([], [], [], [], []));

        var lines = ModActivationPlanText.Render(plan, listFiles: true);

        Assert.Contains("  Move      1 file (renamed within the documents folder)", lines);
        Assert.Contains("  Writes 0 B; that much must be free on the documents folder's drive.", lines);
        Assert.Contains("  mine: move 1", lines);
        Assert.Contains("    > packages/core/override/My Mods/a.dds -> packages/core/override/DAORework/mine/a.dds", lines);
    }
}
