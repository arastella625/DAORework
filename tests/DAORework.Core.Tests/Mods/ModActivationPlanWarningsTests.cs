using DAORework.Core.Mods.Library;
using DAORework.Core.Mods.Planning;
using Xunit;

namespace DAORework.Core.Tests.Mods;

/// <summary>A shared name is a warning only between mods, found with a real mod that keeps a README beside each option.</summary>
public sealed class ModActivationPlanWarningsTests
{
    [Fact]
    public void For_OneModHoldingANameInSeveralFolders_SaysNothing()
    {
        Assert.Empty(Warnings(Mods.Override("zdf", "one/readme.txt#1", "two/readme.txt#2", "three/readme.txt#3")));
    }

    [Fact]
    public void For_TwoMods_NamesEachOnce()
    {
        var warning = Assert.Single(Warnings(Mods.Override("low", "one/hair.dds#1", "two/hair.dds#2"), Mods.Override("high", "hair.dds#3")));

        Assert.StartsWith("hair.dds is in low and high; high's copy is used", warning, StringComparison.Ordinal);
    }

    private static IReadOnlyList<string> Warnings(params LibraryMod[] mods) =>
        ModActivationPlanWarnings.For(ModActivationPlanner.Plan(new PlanInput(DeploymentRecord.Empty, mods, [], new Dictionary<string, string>(), [])).Value, id => id);
}
