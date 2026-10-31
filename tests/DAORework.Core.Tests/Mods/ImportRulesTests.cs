using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Library;
using Xunit;

namespace DAORework.Core.Tests.Mods;

/// <summary>The pure rules behind importing a download (P2-07).</summary>
public sealed class ImportRulesTests
{
    [Theory]
    [InlineData("textures/hair.dds", true)]
    [InlineData("Manifest.xml", true)]
    [InlineData("../evil.txt", false)]
    [InlineData("a/../../evil.txt", false)]
    [InlineData("/etc/passwd", false)]
    [InlineData(@"C:\Windows\evil.dll", false)]
    [InlineData(@"a\b.dds", false)]
    [InlineData("a//b.dds", false)]
    [InlineData("./a.dds", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void ModPath_IsSafe_OnlyForPathsThatStayInsideTheMod(string? path, bool safe)
    {
        Assert.Equal(safe, ModPath.IsSafe(path));
    }

    [Fact]
    public void AddInManifest_TakesTheUidAndTheLocalizedTitle()
    {
        const string xml = "<Manifest><AddInsList><AddInItem UID=\"Extra_Dog_Slot\" Name=\"EDS\"><Title><LocalizedString DefaultText=\"Extra Dog Slot\" /></Title></AddInItem></AddInsList></Manifest>";

        Assert.Equal(new AddInManifest("Extra_Dog_Slot", "Extra Dog Slot"), AddInManifest.Parse(xml).Value);
    }

    [Theory]
    [InlineData("<Manifest><AddInsList><AddInItem UID=\"u\" Name=\"QwinnFixpack3\" /></AddInsList></Manifest>", "QwinnFixpack3")]
    [InlineData("<Manifest><AddInsList><AddInItem UID=\"only_uid\" /></AddInsList></Manifest>", "only_uid")]
    [InlineData("<Manifest><AddInsList><AddInItem UID=\"u\"><Title DefaultText=\"From Title\" /></AddInItem></AddInsList></Manifest>", "From Title")]
    public void AddInManifest_NameFallsBackFromTitleToNameToUid(string xml, string name)
    {
        Assert.Equal(name, AddInManifest.Parse(xml).Value.Name);
    }

    [Theory]
    [InlineData("<Manifest><AddInsList /></Manifest>", "no AddInItem with a UID")]
    [InlineData("<Manifest><AddInsList><AddInItem Name=\"x\" /></AddInsList></Manifest>", "no AddInItem with a UID")]
    [InlineData("<Manifest", "not valid XML")]
    public void AddInManifest_Unusable_Fails(string xml, string message)
    {
        var error = AddInManifest.Parse(xml).Error!;

        Assert.Equal(DiagnosticCodes.ModImportInvalid, error.Code);
        Assert.Contains(message, error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Classify_ManifestAndContents_IsAnAddIn()
    {
        Assert.Equal(new ArchiveClassification(ArchiveShape.AddIn), ArchiveClassifier.Classify(["manifest.xml", "Contents/addins/u/x.gda"]).Value);
    }

    [Fact]
    public void Classify_OneDazipInside_IsAWrappedAddIn()
    {
        Assert.Equal(new ArchiveClassification(ArchiveShape.WrappedAddIn, "QUDAO Fixpack v3_5.dazip"), ArchiveClassifier.Classify(["QUDAO Fixpack v3_5.dazip", "readme.txt"]).Value);
    }

    [Fact]
    public void Classify_LooseFiles_IsAnOverrideMod()
    {
        Assert.Equal(ArchiveShape.Override, ArchiveClassifier.Classify(["Better Hair/hair.dds", "Manifest.xml"]).Value.Shape);
    }

    [Theory]
    [InlineData(new string[0], "the archive is empty")]
    [InlineData(new[] { "a.dazip", "b.dazip" }, "holds 2 .dazip files")]
    [InlineData(new[] { "ok.dds", "../../evil.txt" }, "'../../evil.txt' would be written outside")]
    public void Classify_Unusable_Fails(string[] files, string message)
    {
        var error = ArchiveClassifier.Classify(files).Error!;

        Assert.Equal(DiagnosticCodes.ModImportInvalid, error.Code);
        Assert.Contains(message, error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Qwinn's Ultimate DAO Fixpack", "6A2788E24D900BEA", "qwinn-s-ultimate-dao-fixpack-6a2788e2")]
    [InlineData("Extra Dog Slot", "973f9879f5301581", "extra-dog-slot-973f9879")]
    [InlineData("!!!", "0123456789", "mod-01234567")]
    [InlineData("A very long mod name that goes on and on for far too long", "abcdef0123", "a-very-long-mod-name-that-goes-on-and-on-abcdef01")]
    public void ModIds_Create_IsASlugAndAHashPrefix(string name, string sha, string id)
    {
        Assert.Equal(id, ModIds.Create(name, sha));
    }

    [Theory]
    [InlineData("Better Hair-1234-1-0-1600000000", "Better Hair")]
    [InlineData("QUDAO Fixpack v3.5-4689-3-5-1550475932", "QUDAO Fixpack v3.5")]
    [InlineData("Loose Mod", "Loose Mod")]
    [InlineData("1234", "1234")]
    public void ModIds_NameFromFileName_DropsTheNexusSuffix(string stem, string name)
    {
        Assert.Equal(name, ModIds.NameFromFileName(stem));
    }
}
