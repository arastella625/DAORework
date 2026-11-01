using DAORework.Core.Mods.Conflicts;
using DAORework.Core.Mods.Planning;
using Xunit;

namespace DAORework.Core.Tests.Mods;

/// <summary>What a file two mods ship means, by its kind (P2.5-21).</summary>
public sealed class SharedFileMeaningTests
{
    [Theory]
    [InlineData("hair_01.DDS", "texture or model")]
    [InlineData("hm_har_long_0.mmh", "texture or model")]
    [InlineData("ABI_base.gda", "data table")]
    [InlineData("player_core.ncs", "script")]
    [InlineData("mymod_en-us.tlk", "talk table")]
    [InlineData("module.erf", "archive")]
    [InlineData("settings.xml", "settings file")]
    public void For_KnownKinds_SayWhatIsLost(string fileName, string kind)
    {
        Assert.Contains(kind, SharedFileMeaning.For(fileName), StringComparison.Ordinal);
    }

    [Fact]
    public void For_AKindItDoesNotKnow_SaysNothing()
    {
        Assert.Null(SharedFileMeaning.For("readme.weird"));
    }

    [Fact]
    public void PlanWarning_ATableTwoModsShip_SaysTheOtherChangesAreLost()
    {
        var plan = ModActivationPlanner.Plan(new PlanInput(DeploymentRecord.Empty, [Mods.Override("a", "ABI_base.gda#1"), Mods.Override("b", "ABI_base.gda#2")], [], new Dictionary<string, string>(), [])).Value;

        var warning = Assert.Single(ModActivationPlanWarnings.For(plan, id => id));
        Assert.StartsWith("ABI_base.gda is in a and b; b's copy is used. It is a data table, so the other mod's changes to it are lost.", warning, StringComparison.Ordinal);
    }

    [Fact]
    public void PlanWarning_IdenticalCopies_NeedNoExplanation()
    {
        var plan = ModActivationPlanner.Plan(new PlanInput(DeploymentRecord.Empty, [Mods.Override("a", "ABI_base.gda#1"), Mods.Override("b", "ABI_base.gda#1")], [], new Dictionary<string, string>(), [])).Value;

        Assert.Equal("ABI_base.gda is in a and b; b's copy is used (identical content).", Assert.Single(ModActivationPlanWarnings.For(plan, id => id)));
    }
}
