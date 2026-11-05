using DAORework.Core.Mods.Library;
using DAORework.Core.Mods.Planning;
using Xunit;

namespace DAORework.Core.Tests.Mods;

/// <summary>Files that mean nothing to the game, found in a real library of about 70 mods.</summary>
public sealed class NonModFilesTests
{
    [Theory]
    [InlineData("__folder_managed_by_vortex")]
    [InlineData("textures/__folder_managed_by_vortex")]
    [InlineData(".DS_Store")]
    [InlineData("hair/.DS_Store")]
    [InlineData("._hair.dds")]
    [InlineData("__MACOSX/hair/hair.dds")]
    [InlineData("__macosx/x")]
    [InlineData("Thumbs.db")]
    [InlineData("readme.txt")]
    [InlineData("for chargenmorphcfg.txt")]
    [InlineData("Docs/Install Guide.PDF")]
    public void Is_NotPartOfAMod_True(string path)
    {
        Assert.True(NonModFiles.Is(path));
    }

    [Theory]
    [InlineData("hair.dds")]
    [InlineData("chargenmorphcfg.xml")]
    [InlineData("data/abi_base.gda")]
    [InlineData("readme.txt/hair.dds")]
    [InlineData("MACOSX/hair.dds")]
    public void Is_GameData_False(string path)
    {
        Assert.False(NonModFiles.Is(path));
    }

    [Fact]
    public void Map_KeepsTheModsFiles_AndLeavesTheRestInTheLibrary()
    {
        var mod = Mods.Override("hair", "hair.dds#1", "readme.txt#2", "__folder_managed_by_vortex#3", "__MACOSX/._hair.dds#4");

        var deployed = Assert.Single(ModLayout.Map(mod).Value);

        Assert.Equal("hair.dds", deployed.FileName);
        Assert.Equal(4, mod.Files.Count);
    }

    [Fact]
    public void Plan_AModAlreadyOnWithAReadmePlaced_TakesItOutAndSaysSo()
    {
        const string target = "packages/core/override/DAORework/hair/readme.txt";
        var mod = Mods.Override("hair", "hair.dds#1", "readme.txt#2");
        var current = DeploymentRecord.Empty with { ActiveModIds = ["hair"], Files = [new RecordedFile(target, "2", "hair")] };

        var plan = ModActivationPlanner.Plan(new PlanInput(current, [mod], [], new Dictionary<string, string> { [target] = "2" }, [])).Value;

        Assert.Contains(plan.Operations, o => o.Action == PlanAction.Delete && o.TargetPath == target);
        Assert.Contains("Takes 1 file that means nothing", Assert.Single(PlanNotes.For(plan, id => id)), StringComparison.Ordinal);
    }

    [Fact]
    public void Plan_AReadmeEditedAfterItWasPlaced_StaysAndBlocksNothing()
    {
        const string target = "packages/core/override/DAORework/hair/readme.txt";
        var current = DeploymentRecord.Empty with { ActiveModIds = ["hair"], Files = [new RecordedFile(target, "2", "hair")] };

        var plan = ModActivationPlanner.Plan(new PlanInput(current, [Mods.Override("hair", "readme.txt#2")], [], new Dictionary<string, string> { [target] = "edited" }, [])).Value;

        Assert.False(plan.IsBlocked);
        Assert.Empty(plan.Operations);
        Assert.Empty(plan.NextRecord.Files);
    }

    [Fact]
    public void Plan_TurningAModOff_DoesNotExplainItsReadmeGoing()
    {
        const string target = "packages/core/override/DAORework/hair/readme.txt";
        var current = DeploymentRecord.Empty with { ActiveModIds = ["hair"], Files = [new RecordedFile(target, "2", "hair")] };

        var plan = ModActivationPlanner.Plan(new PlanInput(current, [], [], new Dictionary<string, string> { [target] = "2" }, [])).Value;

        Assert.Empty(PlanNotes.For(plan, id => id));
    }

    [Fact]
    public void Plan_AnAddInAndAnOverrideModBothHoldingAMarker_WarnsAboutNothing()
    {
        var addIn = new LibraryMod("addin", "Add-in", ModKind.AddIn, [new LibraryModFile("Manifest.xml", 1, "m"), new LibraryModFile("Contents/addins/x/__folder_managed_by_vortex", 1, "a")], "uid");
        var plan = ModActivationPlanner.Plan(new PlanInput(DeploymentRecord.Empty, [addIn, Mods.Override("b", "__folder_managed_by_vortex#2")], [], new Dictionary<string, string>(), [])).Value;

        Assert.Empty(ModActivationPlanWarnings.For(plan, id => id));
    }

    [Fact]
    public void Plan_TwoModsSharingAFileInsideAMacFolder_WarnsAboutNothing()
    {
        var plan = ModActivationPlanner.Plan(new PlanInput(DeploymentRecord.Empty, [Mods.Override("a", "__MACOSX/hair.dds#1"), Mods.Override("b", "__MACOSX/hair.dds#2")], [], new Dictionary<string, string>(), [])).Value;

        Assert.Empty(ModActivationPlanWarnings.For(plan, id => id));
    }

    [Fact]
    public void Plan_TwoModsSharingOnlyMarkersAndReadmes_WarnsAboutNothing()
    {
        var plan = ModActivationPlanner.Plan(new PlanInput(
            DeploymentRecord.Empty,
            [Mods.Override("a", "__folder_managed_by_vortex#1", "readme.txt#1", ".DS_Store#1"), Mods.Override("b", "__folder_managed_by_vortex#2", "readme.txt#2", ".DS_Store#2")],
            [],
            new Dictionary<string, string>(),
            [])).Value;

        Assert.Empty(ModActivationPlanWarnings.For(plan, id => id));
    }
}
