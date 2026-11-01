using DAORework.Core.Mods.Backups;
using Xunit;

namespace DAORework.Core.Tests.Mods.Backups;

/// <summary>What a backed-up change did, in a few words (P2.5-19).</summary>
public sealed class DeploymentChangeTextTests
{
    private static readonly Func<string, string> _name = id => id.ToUpperInvariant();

    [Fact]
    public void Describe_ModsAdded_SaysActivatedWithTheirNames()
    {
        Assert.Equal("Activated EYES and NAILS", DeploymentChangeText.Describe(["hair"], ["hair", "eyes", "nails"], moved: false, DeploymentChangeText.Mods, _name));
    }

    [Fact]
    public void Describe_ModsAddedByMovingFiles_SaysAdoptedAndCountsALongList()
    {
        Assert.Equal(
            "Adopted 4 mods: A, B, C and 1 more",
            DeploymentChangeText.Describe([], ["a", "b", "c", "d"], moved: true, DeploymentChangeText.Mods, _name));
    }

    [Fact]
    public void Describe_AddedAndRemoved_SaysBoth()
    {
        Assert.Equal("Activated NAILS; deactivated EYES", DeploymentChangeText.Describe(["eyes"], ["nails"], moved: false, DeploymentChangeText.Mods, _name));
    }

    [Fact]
    public void Describe_SameModsInAnotherOrder_SaysTheLoadOrderChanged()
    {
        Assert.Equal("Changed the load order", DeploymentChangeText.Describe(["a", "b"], ["b", "a"], moved: false, DeploymentChangeText.Mods, _name));
    }

    [Fact]
    public void Describe_NothingDifferent_SaysSo()
    {
        Assert.Equal("Changed no active mods", DeploymentChangeText.Describe(["a"], ["a"], moved: false, DeploymentChangeText.Mods, _name));
    }

    [Fact]
    public void Describe_Fixes_UseTheirOwnWords()
    {
        Assert.Equal("Removed DXVK", DeploymentChangeText.Describe(["dxvk"], [], moved: false, DeploymentChangeText.Fixes, _name));
        Assert.Equal("Applied LAA", DeploymentChangeText.Describe([], ["laa"], moved: false, DeploymentChangeText.Fixes, _name));
    }
}
