using DAORework.Core.Mods.Library;
using DAORework.Core.Mods.Planning;
using Xunit;

namespace DAORework.Core.Tests.Mods;

/// <summary>A preview's warnings: the full sentences for a small plan, a few grouped lines for a large one.</summary>
public sealed class PlanNotesTests
{
    [Fact]
    public void For_ASmallPlan_KeepsTheSentence()
    {
        var note = Assert.Single(Notes(Mods.Override("red", "hair.dds#1"), Mods.Override("blue", "hair.dds#2")));

        Assert.StartsWith("hair.dds is in red and blue; blue's copy is used.", note, StringComparison.Ordinal);
    }

    [Fact]
    public void For_IdenticalCopies_SaysNothing()
    {
        Assert.Empty(Notes(Mods.Override("red", "hair.dds#1"), Mods.Override("blue", "hair.dds#1")));
    }

    [Fact]
    public void For_HundredsOfSharedFiles_GroupsByTheModsInvolved()
    {
        var notes = Notes(Mods.Override("pack", Textures(300, "a")), Mods.Override("armor", Textures(300, "b")));

        Assert.Equal(
            [
                "300 files are in more than one mod. For each one, the mod lower in the order wins.",
                "armor wins over pack: 300 textures and models.",
            ],
            notes);
    }

    [Fact]
    public void For_SharedTablesAndScripts_SaysWhatIsAtStake_AndPutsThemFirst()
    {
        var notes = Notes(
            Mods.Override("pack", [.. Textures(50, "a"), "x.dds#z"]),
            Mods.Override("armor", Textures(50, "b")),
            Mods.Override("rules", "abi_base.gda#1", "party.ncs#1", "x.dds#y"),
            Mods.Override("talents", "abi_base.gda#2", "party.ncs#2"));

        Assert.Equal("2 of them are data tables, scripts or talk tables, where a mod's changes are lost. Look for a compatibility patch.", notes[1]);
        Assert.Equal("talents wins over rules: 1 data table and 1 script.", notes[2]);
        Assert.Equal("armor wins over pack: 50 textures and models.", notes[3]);
    }

    [Fact]
    public void For_ManySetsOfMods_ShowsThreeAndCountsTheRest()
    {
        var mods = Enumerable.Range(0, 10).SelectMany(i => new[] { Mods.Override($"low{i}", $"f{i}.dds#1"), Mods.Override($"high{i}", $"f{i}.dds#2") }).ToArray();

        var notes = Notes(mods);

        Assert.Equal(5, notes.Count);
        Assert.Equal("7 more sets of mods share files. Show details lists them.", notes[^1]);
    }

    [Fact]
    public void For_AFileInManyMods_NamesThreeAndCountsTheRest()
    {
        var mods = Enumerable.Range(0, 8).Select(i => Mods.Override($"m{i}", Textures(10, i.ToString(System.Globalization.CultureInfo.InvariantCulture)))).ToArray();

        Assert.Equal("m7 wins over m0, m1, m2 and 4 more: 10 textures and models.", Notes(mods)[1]);
    }

    [Fact]
    public void For_OneSharedFileAmongManyWarnings_WritesItInTheSingular()
    {
        var plan = ModActivationPlanner.Plan(new PlanInput(DeploymentRecord.Empty, [Mods.Override("a", "x.dds#1"), Mods.Override("b", "x.dds#2")], [], new Dictionary<string, string>(), [])).Value with
        {
            CombineProblems = ["one", "two", "three", "four", "five", "six"],
        };

        Assert.Equal("1 file is in more than one mod. The mod lower in the order wins.", PlanNotes.For(plan, id => id)[0]);
    }

    private static string[] Textures(int count, string content) => [.. Enumerable.Range(0, count).Select(i => $"t{i}.dds#{content}")];

    private static IReadOnlyList<string> Notes(params LibraryMod[] mods) =>
        PlanNotes.For(ModActivationPlanner.Plan(new PlanInput(DeploymentRecord.Empty, mods, [], new Dictionary<string, string>(), [])).Value, id => id);
}
