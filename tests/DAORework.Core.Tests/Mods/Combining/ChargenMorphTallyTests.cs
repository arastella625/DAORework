using DAORework.Core.Mods.Combining;
using Xunit;

namespace DAORework.Core.Tests.Mods.Combining;

/// <summary>What a mod adds to character creation (P2.10-03, ADR-0050 §4). The samples are made up here, not the game's file.</summary>
public sealed class ChargenMorphTallyTests
{
    private const string HumanHair = """<resource name="hm_har_a" cut="0" />""";
    private const string ElfHair = """<resource name="ef_har_a" cut="0" />""";
    private const string HumanHead = """<resource name="hm_hed_a" />""";

    private const string Game = """
        <morph_config>
          <hairs>
            <human_male><resource name="hm_har_a" cut="0" /></human_male>
            <elf_female><resource name="ef_har_a" cut="0" /></elf_female>
          </hairs>
          <heads><human_male><resource name="hm_hed_a" /></human_male></heads>
          <hair_colors><resource name="t3_har_1" /></hair_colors>
        </morph_config>
        """;

    [Fact]
    public void AddsOf_CountsWhatTheGamesCopyLacks_ByKind()
    {
        var hair = With("""<resource name="hm_har_long" cut="0" /><resource name="hm_har_long" cut="1" />""", HumanHair);
        var faces = With("""<resource name="hm_hed_b" /><resource name="hm_hed_c" />""", HumanHead)
            .Replace("<resource name=\"t3_har_1\" />", "<resource name=\"t3_har_1\" /><resource name=\"t3_har_2\" />", StringComparison.Ordinal);

        var tally = new ChargenMorphTally(Copies(("hair", hair), ("faces", faces)), Read(Game));

        Assert.True(tally.AgainstTheGame);
        Assert.Equal("Adds 2 hairstyles", tally.AddsOf("hair")!.Adds()); // The same hair with another cut is two.
        Assert.Equal("Adds 2 faces and 1 colour", tally.AddsOf("faces")!.Adds());
        Assert.Null(tally.AddsOf("not-a-chargen-mod"));
    }

    [Fact]
    public void AddsOf_TheSameHairForAnotherRace_IsANewEntry()
    {
        var elf = With("""<resource name="hm_har_a" cut="0" />""", ElfHair);

        Assert.Equal("Adds 1 hairstyle", new ChargenMorphTally(Copies(("elf", elf)), Read(Game)).AddsOf("elf")!.Adds());
    }

    [Fact]
    public void AddsOf_WithoutTheGamesCopy_CountsAgainstWhatEveryCopyShares()
    {
        var one = With("""<resource name="hm_har_b" cut="0" />""", HumanHair);
        var two = With("""<resource name="hm_har_c" cut="0" /><resource name="hm_har_d" cut="0" />""", HumanHair);

        var tally = new ChargenMorphTally(Copies(("one", one), ("two", two)), game: null);

        Assert.False(tally.AgainstTheGame);
        Assert.Equal("Adds 1 hairstyle", tally.AddsOf("one")!.Adds());
        Assert.Equal("Adds 2 hairstyles", tally.AddsOf("two")!.Adds());
    }

    [Fact]
    public void AddsOf_OneModAndNoGameCopy_IsNotGuessed()
    {
        var tally = new ChargenMorphTally(Copies(("only", Game)), game: null);

        Assert.Equal("Adds to character creation", tally.AddsOf("only")!.Adds());
        Assert.Null(tally.Gained([], ["only"])!.Gained());
    }

    [Fact]
    public void AddsOf_ACopyThatAddsNothing_SaysNothing_AndASectionWithoutAWord_StillAdds()
    {
        var unknown = Game.Replace("</morph_config>", "<voices><resource name=\"v1\" /></voices></morph_config>", StringComparison.Ordinal);

        var tally = new ChargenMorphTally(Copies(("same", Game), ("voices", unknown)), Read(Game));

        Assert.Null(tally.AddsOf("same")!.Adds());
        Assert.Equal("Adds to character creation", tally.AddsOf("voices")!.Adds());
    }

    [Fact]
    public void AddsOf_TwoModsWithTheSameList_AndNoGameCopy_StillAddToCharacterCreation()
    {
        var hairs = With("<resource name=\"hm_har_b\" cut=\"0\" />", HumanHair);

        var tally = new ChargenMorphTally(Copies(("1.0", hairs), ("1.1", hairs)), game: null);

        Assert.Equal("Adds to character creation", tally.AddsOf("1.0")!.Adds());
    }

    [Fact]
    public void Gained_CountsOnlyWhatTheChangeBroughtThatWasntThereBefore()
    {
        var a = With("""<resource name="hm_har_b" cut="0" /><resource name="hm_har_c" cut="0" />""", HumanHair);
        var b = With("""<resource name="hm_har_c" cut="0" /><resource name="hm_har_d" cut="0" />""", HumanHair);
        var tally = new ChargenMorphTally(Copies(("a", a), ("b", b)), Read(Game));

        Assert.Equal("Character creation has 2 more hairstyles", tally.Gained([], ["a"])!.Gained());
        Assert.Equal("Character creation has 1 more hairstyle", tally.Gained(["a"], ["a", "b"])!.Gained());
        Assert.Null(tally.Gained(["a", "b"], ["a"])!.Gained()); // Turning one off gains nothing.
        Assert.Null(tally.Gained([], ["not-a-chargen-mod"]));
    }

    [Fact]
    public void AddsOf_AHairColourMod_CountsEachColourOnce_ThoughItFillsThreeLists()
    {
        // The game's own section names (checked 2026-11-23); every entry here is made up.
        const string game = """
            <morph_config>
              <heads><human_male><resource name="hm_hed_a" /></human_male></heads>
              <hair_colors><resource name="t1_har_a" /></hair_colors>
              <brow_stubble_colors><resource name="t1_brw_a" /></brow_stubble_colors>
              <crew_cut_colors><resource name="t1_crw_a" /></crew_cut_colors>
              <lip_makeup_colors><resource name="t1_lip_a" /></lip_makeup_colors>
            </morph_config>
            """;
        var tints = game
            .Replace("<resource name=\"t1_har_a\" />", "<resource name=\"t1_har_a\" /><resource name=\"t1_har_b\" /><resource name=\"t1_har_c\" />", StringComparison.Ordinal)
            .Replace("<resource name=\"t1_brw_a\" />", "<resource name=\"t1_brw_a\" /><resource name=\"t1_brw_b\" /><resource name=\"t1_brw_c\" />", StringComparison.Ordinal)
            .Replace("<resource name=\"t1_crw_a\" />", "<resource name=\"t1_crw_a\" /><resource name=\"t1_crw_b\" /><resource name=\"t1_crw_c\" />", StringComparison.Ordinal)
            .Replace("<resource name=\"t1_lip_a\" />", "<resource name=\"t1_lip_a\" /><resource name=\"t1_lip_b\" />", StringComparison.Ordinal);

        var tally = new ChargenMorphTally(Copies(("tints", tints)), Read(game));

        Assert.Equal("Adds 3 colours", tally.AddsOf("tints")!.Adds()); // 2 hair colours and 1 lipstick, not 2 brow or crew-cut ones too.
    }

    [Fact]
    public void Read_NotAMorphConfig_IsNotCounted()
    {
        Assert.Null(ChargenMorphEntries.Read("<other/>"));
        Assert.Null(ChargenMorphEntries.Read("<morph_config>"));
        Assert.Null(ChargenMorphEntries.Read("<!DOCTYPE morph_config [<!ENTITY x \"y\">]><morph_config/>"));
    }

    private static string With(string entries, string after) => Game.Replace(after, after + entries, StringComparison.Ordinal);

    private static ChargenMorphEntries Read(string xml) => ChargenMorphEntries.Read(xml) ?? throw new InvalidOperationException("The sample doesn't read.");

    private static Dictionary<string, ChargenMorphEntries> Copies(params (string Id, string Xml)[] copies) =>
        copies.ToDictionary(c => c.Id, c => Read(c.Xml), StringComparer.Ordinal);
}
