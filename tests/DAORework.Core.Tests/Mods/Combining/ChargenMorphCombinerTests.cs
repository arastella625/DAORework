using System.Xml.Linq;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Combining;
using Xunit;

namespace DAORework.Core.Tests.Mods.Combining;

/// <summary>Combining chargenmorphcfg.xml from several mods (ADR-0025). The samples are made up here, not the game's file.</summary>
public sealed class ChargenMorphCombinerTests
{
    private const string Base = """
        <morph_config>
          <hairs>
            <human_male>
              <resource name="hm_har_test_a" cut="0" />
              <resource name="hm_har_test_b" cut="1" />
            </human_male>
          </hairs>
          <hair_colors>
            <resource name="" />
            <resource name="t3_har_test_1" />
          </hair_colors>
        </morph_config>
        """;

    [Fact]
    public void Combine_TwoMods_KeepsTheFirstOrderAndAddsTheOthersEntriesAfter()
    {
        var hairs = Base.Replace("""<resource name="hm_har_test_b" cut="1" />""", """<resource name="hm_har_test_b" cut="1" /><resource name="hm_har_mod_c" cut="0" />""", StringComparison.Ordinal);
        var colours = Base.Replace("""<resource name="t3_har_test_1" />""", """<resource name="t3_har_test_1" /><resource name="t3_har_mod_2" />""", StringComparison.Ordinal);

        var combined = Combine(("hairs-1", hairs), ("colours-2", colours));

        Assert.Equal(["hm_har_test_a", "hm_har_test_b", "hm_har_mod_c"], Names(combined, "hairs", "human_male"));
        Assert.Equal([string.Empty, "t3_har_test_1", "t3_har_mod_2"], Names(combined, "hair_colors"));
        Assert.Equal(6, combined.EntryCount);
        Assert.Equal(["hairs-1", "colours-2"], combined.ModIds);
    }

    [Fact]
    public void Combine_TheSameHairWithAnotherCut_IsKept_AndAnIdenticalOneIsNot()
    {
        var other = Base.Replace("""<resource name="hm_har_test_b" cut="1" />""", """<resource name="HM_HAR_TEST_B" CUT="1" /><resource name="hm_har_test_b" cut="0234" />""", StringComparison.Ordinal);

        var combined = Combine(("a-1", Base), ("b-2", other));

        Assert.Equal(["hm_har_test_a", "hm_har_test_b", "hm_har_test_b"], Names(combined, "hairs", "human_male"));
        Assert.Contains("cut=\"0234\"", combined.Xml, StringComparison.Ordinal);
    }

    [Fact]
    public void Combine_ASectionOrGroupOnlyALaterModHas_IsAddedWithItsEntries()
    {
        var elves = Base.Replace("</hairs>", """<elf_female><resource name="ef_har_mod_a" cut="0" /></elf_female></hairs><beards><dwarf_male><resource name="" /></dwarf_male></beards>""", StringComparison.Ordinal);

        var combined = Combine(("a-1", Base), ("elves-2", elves));

        Assert.Equal(["ef_har_mod_a"], Names(combined, "hairs", "elf_female"));
        Assert.Equal([string.Empty], Names(combined, "beards", "dwarf_male"));
    }

    [Fact]
    public void Combine_ElementsAndAttributesItDoesNotKnow_AreKept()
    {
        var odd = Base.Replace("<hair_colors>", """<hair_colors><resource name="t3_har_mod_x" tint="warm" /></hair_colors><future_section note="kept"><item value="1" /></future_section><hair_colors>""", StringComparison.Ordinal);

        var combined = Combine(("a-1", Base), ("odd-2", odd));

        Assert.Contains("""tint="warm" """.TrimEnd(), combined.Xml, StringComparison.Ordinal);
        Assert.Contains("""<future_section note="kept">""", combined.Xml, StringComparison.Ordinal);
    }

    [Fact]
    public void Combine_IsTheSameEveryTime()
    {
        var first = Combine(("a-1", Base), ("b-2", Base));
        var second = Combine(("a-1", Base), ("b-2", Base));

        Assert.Equal(first.Xml, second.Xml);
        Assert.StartsWith("<?xml version=\"1.0\" encoding=\"utf-8\"?>", first.Xml, StringComparison.Ordinal);
        Assert.Equal(4, first.EntryCount);
    }

    [Theory]
    [InlineData("<morph_config><hairs>", "unclosed")]
    [InlineData("<config><hairs /></config>", "root")]
    [InlineData("<!DOCTYPE morph_config [<!ENTITY x SYSTEM \"file:///c:/secret.txt\">]><morph_config>&x;</morph_config>", "DTD")]
    public void Combine_ACopyItCannotRead_IsLeftOutAndNamed_AndTheOthersAreCombined(string xml, string why)
    {
        var result = ChargenMorphCombiner.Combine([new ChargenMorphCopy("good-1", Base), new ChargenMorphCopy("broken-2", xml, "Broken Hair"), new ChargenMorphCopy("good-3", Base)]);

        Assert.True(result.IsSuccess, why);
        Assert.Equal(["good-1", "good-3"], result.Value.ModIds);
        var leftOut = Assert.Single(result.Value.LeftOut);
        Assert.Equal(DiagnosticCodes.ModChargenMorphUnreadable, leftOut.Code);
        Assert.StartsWith("chargenmorphcfg.xml from Broken Hair cannot be read: ", leftOut.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("..", leftOut.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Combine_NoCopyItCanRead_HasNothingToCombine()
    {
        var result = ChargenMorphCombiner.Combine([new ChargenMorphCopy("broken-1", "<morph_config>"), new ChargenMorphCopy("broken-2", "<x />")]);

        Assert.False(result.IsSuccess);
        Assert.Equal(DiagnosticCodes.ModChargenMorphUnreadable, result.Error!.Code);
    }

    [Fact]
    public void Combine_EntriesAddedLater_AreIndentedLikeTheRest()
    {
        var other = Base.Replace("""<resource name="t3_har_test_1" />""", """<resource name="t3_har_test_1" /><resource name="t3_har_mod_2" />""", StringComparison.Ordinal);

        var lines = Combine(("a-1", Base), ("b-2", other)).Xml.Split("\r\n");

        Assert.Contains("    <resource name=\"t3_har_mod_2\" />", lines);
        Assert.Equal(5, lines.Count(l => l.TrimStart().StartsWith("<resource", StringComparison.Ordinal)));
    }

    private static CombinedChargenMorph Combine(params (string ModId, string Xml)[] copies)
    {
        var result = ChargenMorphCombiner.Combine([.. copies.Select(c => new ChargenMorphCopy(c.ModId, c.Xml))]);
        Assert.True(result.IsSuccess, result.Error?.Message);
        return result.Value;
    }

    private static List<string> Names(CombinedChargenMorph combined, params string[] path)
    {
        var element = XDocument.Parse(combined.Xml).Root!;
        foreach (var name in path)
        {
            element = element.Element(name)!;
        }

        return [.. element.Elements("resource").Select(e => (string)e.Attribute("name")!)];
    }
}
