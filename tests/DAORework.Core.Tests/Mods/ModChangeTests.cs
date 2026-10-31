using DAORework.Core.Mods.Planning;
using Xunit;

namespace DAORework.Core.Tests.Mods;

public sealed class ModChangeTests
{
    private static readonly string[] _active = ["ui", "hair"];

    [Fact]
    public void Activate_AppendsNewModsAfterTheActiveOnesSoTheyWin()
    {
        Assert.Equal(["ui", "hair", "fixpack", "dog"], new ModChange(ModChangeKind.Activate, ["fixpack", "HAIR", "dog", "fixpack"]).Apply(_active));
    }

    [Fact]
    public void Deactivate_RemovesOnlyTheNamedMods()
    {
        Assert.Equal(["hair"], new ModChange(ModChangeKind.Deactivate, ["UI", "not-active"]).Apply(_active));
    }

    [Fact]
    public void Set_ReplacesTheListInTheGivenOrder()
    {
        Assert.Equal(["dog", "ui"], new ModChange(ModChangeKind.Set, ["dog", "ui", "DOG"]).Apply(_active));
    }
}
