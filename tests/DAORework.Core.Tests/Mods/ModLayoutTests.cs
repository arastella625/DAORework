using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Library;
using Xunit;

namespace DAORework.Core.Tests.Mods;

public sealed class ModLayoutTests
{
    [Fact]
    public void Map_OverrideMod_GoesUnderTheOwnedSubfolderKeepingItsLayout()
    {
        var mod = Mods.Override("better-hair-1a2b3c4d", "textures/hair_01.dds", "readme.txt");

        var files = ModLayout.Map(mod).Value;

        Assert.Equal(
            ["packages/core/override/DAORework/better-hair-1a2b3c4d/textures/hair_01.dds", "packages/core/override/DAORework/better-hair-1a2b3c4d/readme.txt"],
            files.Select(f => f.TargetPath));
        Assert.All(files, f => Assert.True(f.IsOverride));
        Assert.Equal("hair_01.dds", files[0].FileName);
    }

    /// <summary>The layout the owner's Extra Dog Slot and Qwinn's Fixpack downloads showed (P1-07).</summary>
    [Fact]
    public void Map_AddIn_FollowsDaUpdaterExceptOverrideGoesToTheOwnedSubfolder()
    {
        var mod = Mods.AddIn(
            "extra-dog-slot-973f9879",
            "Extra_Dog_Slot",
            "Manifest.xml",
            "Contents/addins/Extra_Dog_Slot/module/data/dog.gda",
            "Contents/packages/core/override/dog_whistle.uti",
            "Contents/packages/core/data/talktables/core_en-us.tlk");

        var files = ModLayout.Map(mod).Value;

        Assert.Equal(
            [
                "AddIns/Extra_Dog_Slot/module/data/dog.gda",
                "packages/core/override/DAORework/extra-dog-slot-973f9879/dog_whistle.uti",
                "packages/core/data/talktables/core_en-us.tlk",
            ],
            files.Select(f => f.TargetPath));
        Assert.Equal([false, true, false], files.Select(f => f.IsOverride));
    }

    [Theory]
    [InlineData("../escape.dds")]
    [InlineData("textures/../../escape.dds")]
    [InlineData("/rooted.dds")]
    [InlineData(@"C:\Windows\x.dll")]
    [InlineData(@"textures\hair.dds")]
    [InlineData("textures//hair.dds")]
    [InlineData("./hair.dds")]
    [InlineData(" ")]
    public void Map_PathThatWouldLeaveTheModFolder_Fails(string path)
    {
        var result = ModLayout.Map(Mods.Override("bad", path));

        Assert.Equal(DiagnosticCodes.ModPathInvalid, result.Error!.Code);
    }
}
