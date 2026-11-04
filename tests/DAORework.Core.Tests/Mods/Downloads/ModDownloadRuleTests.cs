using DAORework.Core.Mods.Downloads;
using Xunit;

namespace DAORework.Core.Tests.Mods.Downloads;

/// <summary>What counts as a new mod, from names only (ADR-0034 §4).</summary>
public sealed class ModDownloadRuleTests
{
    private static readonly DateTimeOffset _on = new(2026, 11, 4, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("Hair/thing.erf")]
    [InlineData("a.GDA")]
    [InlineData("talk.tlk")]
    [InlineData("script.ncs")]
    [InlineData("item.uti")]
    [InlineData("creature.utc")]
    [InlineData("model.mmh")]
    [InlineData("mesh.msh")]
    [InlineData("material.mao")]
    [InlineData("Presets/face.mop")]
    [InlineData("Tints/hair.TNT")]
    [InlineData("speech.dlg")]
    [InlineData("speech.dlb")]
    [InlineData("kiss.cut")]
    [InlineData("Mod/chargenmorphcfg.xml")]
    [InlineData("Inner.dazip")]
    public void LooksLikeMod_TheGamesOwnData_Passes(string entry)
    {
        Assert.True(ModDownloadRule.LooksLikeMod(["readme.txt", entry]));
    }

    [Fact]
    public void LooksLikeMod_AnAddInsLayout_Passes()
    {
        Assert.True(ModDownloadRule.LooksLikeMod(["Manifest.xml", "Contents/addins/x/y.txt"]));
    }

    [Theory]
    [InlineData("texture.dds")]
    [InlineData("setup.exe")]
    [InlineData("readme.txt")]
    public void LooksLikeMod_AnythingElse_DoesNot(string entry)
    {
        Assert.False(ModDownloadRule.LooksLikeMod(["notes.pdf", entry]));
    }

    [Theory]
    [InlineData("a.dazip", true)]
    [InlineData("a.ZIP", true)]
    [InlineData("a.7z", true)]
    [InlineData("a.rar", true)]
    [InlineData("a.crdownload", false)]
    [InlineData("a.zip.part", false)]
    [InlineData("a.exe", false)]
    public void IsDownload_TakesWhatAddModsTakes(string path, bool expected)
    {
        Assert.Equal(expected, ModDownloadRule.IsDownload(path));
    }

    [Fact]
    public void IsNew_OnlyAfterItWasTurnedOn_ByTheLaterOfItsTimes()
    {
        var early = _on.AddDays(-1);
        var late = _on.AddMinutes(1);

        Assert.False(ModDownloadRule.IsNew(new DownloadFile("a.zip", 1, early, early), _on, []));
        Assert.True(ModDownloadRule.IsNew(new DownloadFile("a.zip", 1, late, early), _on, []));
        Assert.True(ModDownloadRule.IsNew(new DownloadFile("a.zip", 1, early, late), _on, []));
        Assert.True(ModDownloadRule.IsNew(new DownloadFile("a.zip", 1, early, early), null, []));
    }

    [Fact]
    public void IsNew_AFileDealtWith_IsNot_UntilItChanges()
    {
        var file = new DownloadFile("C:/D/a.zip", 5, _on, _on);
        HandledDownload[] handled = [HandledDownload.From(file)];

        Assert.False(ModDownloadRule.IsNew(file, null, handled));
        Assert.True(ModDownloadRule.IsNew(file with { SizeBytes = 6 }, null, handled));
        Assert.True(ModDownloadRule.IsNew(file with { ChangedUtc = _on.AddSeconds(1) }, null, handled));
    }
}
