using DAORework.Core.Mods.Adoption;
using DAORework.Core.Mods.Planning;
using Xunit;

namespace DAORework.Core.Tests.Mods;

/// <summary>What a plan and the list say about the install folder's override (P2.5-22).</summary>
public sealed class InstallOverrideWarningsTests
{
    [Fact]
    public void PlanWarning_AFileTheInstallFolderHasToo_SaysTheGameUsesOneCopy_WhateverItsCase()
    {
        var warning = Assert.Single(ModActivationPlanWarnings.For(Plan(["hair.dds"], @"Old Hair\HAIR.DDS"), id => id));

        Assert.StartsWith("hair.dds from a is also in your game folder's override, which DAORework does not manage; the game uses one copy.", warning, StringComparison.Ordinal);
        Assert.Contains("texture or model", warning, StringComparison.Ordinal);
    }

    [Fact]
    public void Plan_ANameTheInstallFolderDoesNotHave_SaysNothing()
    {
        Assert.Empty(ModActivationPlanWarnings.For(Plan(["hair.dds"], "other.dds"), id => id));
    }

    [Fact]
    public void Plan_NeverBlocksOverTheInstallFolder()
    {
        Assert.False(Plan(["hair.dds"], "hair.dds").IsBlocked);
    }

    [Fact]
    public void PlanWarning_ManyFiles_NamesTenAndCountsTheRest()
    {
        var names = Enumerable.Range(1, 12).Select(i => $"f{i}.dds").ToArray();

        var warnings = ModActivationPlanWarnings.For(Plan(names, names), id => id);

        Assert.Equal(11, warnings.Count);
        Assert.Equal("2 files more are in your game folder's override too.", warnings[^1]);
    }

    [Fact]
    public void PlanWarning_AFolderThatCouldNotBeRead_SaysSo()
    {
        var input = new PlanInput(DeploymentRecord.Empty, [], [], new Dictionary<string, string>(), []) { InstallOverrideProblem = "Couldn't read it." };

        Assert.Equal("Couldn't read it.", Assert.Single(ModActivationPlanWarnings.For(ModActivationPlanner.Plan(input).Value, id => id)));
    }

    [Fact]
    public void Contents_FoldersInOrder_LooseFilesLast()
    {
        var contents = InstallOverrideContents.From([new("z.dds", 1), new("b/x.dds", 2), new("A/y.dds", 3), new("b/w.dds", 4)]);

        Assert.Equal(["A", "b", InstallOverrideContents.LooseFilesName], contents.Mods.Select(m => m.Name));
        Assert.Equal(6, contents.Mods[1].SizeBytes);
    }

    private static ModActivationPlan Plan(string[] files, params string[] installOverride) =>
        ModActivationPlanner.Plan(new PlanInput(DeploymentRecord.Empty, [Mods.Override("a", files.Select(f => f + "#1").ToArray())], [], new Dictionary<string, string>(), [])
        {
            InstallOverridePaths = installOverride,
        }).Value;
}
