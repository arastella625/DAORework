using DAORework.Core.Mods.Library;
using Xunit;

namespace DAORework.Core.Tests.Mods;

public sealed class KeepOneOfEachTests
{
    private const string Here = "here";
    private const string There = "there";

    private static readonly Dictionary<string, string?> _noPicks = [];

    private static readonly LibraryEntry _newHair = Entry("new-hair", "Hair", 2, ("hair.dds", "aa", 10));
    private static readonly LibraryEntry _oldHair = Entry("old-hair", "Hair", 1, ("hair.dds", "aa", 10));
    private static readonly LibraryEntry _newEyes = Entry("new-eyes", "Eyes", 2, ("eyes.dds", "bb", 20));
    private static readonly LibraryEntry _oldEyes = Entry("old-eyes", "Eyes", 1, ("eyes.dds", "bb", 20));
    private static readonly LibraryEntry _alone = Entry("alone", "Alone", 1, ("alone.dds", "cc", 5));

    [Fact]
    public void Plan_NothingOn_KeepsTheNewestOfEach_AndCountsTheSpace()
    {
        var plan = KeepOneOfEach.Plan([_oldHair, _newHair, _oldEyes, _newEyes, _alone], _noPicks, Here, [new FolderMods(Here, [], false)]);

        Assert.Equal(["new-eyes", "new-hair"], plan.Sets.Select(s => s.Plan.Kept.Mod.Id));
        Assert.Equal(["old-eyes", "old-hair"], plan.Removing.Select(e => e.Mod.Id));
        Assert.Equal(30, plan.BytesFreed);
        Assert.Null(plan.SwappedOrder);
    }

    [Fact]
    public void Plan_ACopyOn_StartsOnThatOne_SoNothingInTheGameChanges()
    {
        var plan = KeepOneOfEach.Plan([_oldHair, _newHair], _noPicks, Here, [new FolderMods(Here, ["old-hair"], false)]);

        Assert.Equal("old-hair", Assert.Single(plan.Sets).Plan.Kept.Mod.Id);
        Assert.Null(plan.SwappedOrder);
    }

    [Fact]
    public void Plan_ACopyOnInAnotherFolder_StartsOnThatOne_SoItIsNotStopped()
    {
        var plan = KeepOneOfEach.Plan([_oldHair, _newHair], _noPicks, Here, [new FolderMods(Here, [], false), new FolderMods(There, ["old-hair"], false)]);

        var set = Assert.Single(plan.Sets);
        Assert.Equal("old-hair", set.Plan.Kept.Mod.Id);
        Assert.True(set.Clears);
    }

    [Fact]
    public void Plan_TwoCopiesOn_TheNewestStays_AndTheOtherIsTakenOut()
    {
        var plan = KeepOneOfEach.Plan([_oldHair, _newHair], _noPicks, Here, [new FolderMods(Here, ["old-hair", "a", "new-hair"], false)]);

        Assert.Equal("new-hair", Assert.Single(plan.Sets).Plan.Kept.Mod.Id);
        Assert.Equal(["a", "new-hair"], plan.SwappedOrder);
    }

    [Fact]
    public void Plan_EverySwap_GoesInOneOrder_InTheSamePlaces()
    {
        var picks = new Dictionary<string, string?> { [Group(_newHair)] = "new-hair", [Group(_newEyes)] = "new-eyes" };

        var plan = KeepOneOfEach.Plan([_oldHair, _newHair, _oldEyes, _newEyes], picks, Here, [new FolderMods(Here, ["old-eyes", "a", "old-hair"], false)]);

        Assert.Equal(["new-eyes", "a", "new-hair"], plan.SwappedOrder);
    }

    [Fact]
    public void Plan_KeepAll_LeavesThatSetAlone_AndTheRestGoAhead()
    {
        var picks = new Dictionary<string, string?> { [Group(_newHair)] = null };

        var plan = KeepOneOfEach.Plan([_oldHair, _newHair, _oldEyes, _newEyes], picks, Here, [new FolderMods(Here, ["old-hair"], false)]);

        Assert.Equal(2, plan.Sets.Count);
        Assert.True(plan.Sets.Single(s => s.Copies.Contains(_newHair)).KeepsAll);
        Assert.Equal("old-eyes", Assert.Single(plan.Removing).Mod.Id);
        Assert.Null(plan.SwappedOrder);
    }

    [Fact]
    public void Plan_APickNoLongerInTheSet_FallsBackToTheStart()
    {
        var picks = new Dictionary<string, string?> { [Group(_newHair)] = "gone" };

        var plan = KeepOneOfEach.Plan([_oldHair, _newHair], picks, Here, []);

        Assert.Equal("new-hair", Assert.Single(plan.Sets).Plan.Kept.Mod.Id);
    }

    [Fact]
    public void Plan_ASetOnInTwoFolders_StaysAsItIs_AndTheOthersGoAhead()
    {
        var plan = KeepOneOfEach.Plan(
            [_oldHair, _newHair, _oldEyes, _newEyes],
            _noPicks,
            Here,
            [new FolderMods(Here, ["new-hair"], false), new FolderMods(There, ["old-hair"], false)]);

        var hair = plan.Sets.Single(s => s.Copies.Contains(_newHair));
        Assert.False(hair.Clears);
        Assert.Contains(There, Assert.Single(hair.Plan.Problems).Title, StringComparison.Ordinal);
        Assert.Equal("old-eyes", Assert.Single(plan.Removing).Mod.Id);
        Assert.Empty(plan.Problems);
    }

    [Fact]
    public void Plan_AChangeThatDidNotFinish_IsSaidOnce_NotOnEverySet()
    {
        var plan = KeepOneOfEach.Plan([_oldHair, _newHair, _oldEyes, _newEyes], _noPicks, Here, [new FolderMods(Here, [], true)]);

        Assert.Contains("didn't finish", Assert.Single(plan.Problems).Title, StringComparison.Ordinal);
        Assert.All(plan.Sets, s => Assert.Empty(s.Plan.Problems));
    }

    [Fact]
    public void Plan_CopiesThatArentTheSame_ComeFirst_AndStayThereWhateverIsPicked()
    {
        var newAddIn = AddIn("new-zeta", 2, ("zeta.erf", "dd", 10), ("more.erf", "ee", 5));
        var oldAddIn = AddIn("old-zeta", 1, ("zeta.erf", "dd", 10));
        LibraryEntry[] entries = [_oldHair, _newHair, oldAddIn, newAddIn];

        var first = KeepOneOfEach.Plan(entries, _noPicks, Here, []);
        var picks = new Dictionary<string, string?> { [first.Sets[0].Group] = "new-zeta" };
        var kept = KeepOneOfEach.Plan(entries, picks, Here, []);

        Assert.True(first.Sets[0].Differs);
        Assert.False(first.Sets[1].Differs);
        Assert.Equal(first.Sets.Select(s => s.Group), kept.Sets.Select(s => s.Group));
        Assert.True(kept.Sets[0].Differs);
        Assert.Empty(kept.Sets[0].Plan.OnlyInRemoved);
    }

    [Fact]
    public void Plan_AFileOnlyInARemovedCopy_IsNamed()
    {
        var newAddIn = AddIn("new-zeta", 2, ("zeta.erf", "dd", 10));
        var oldAddIn = AddIn("old-zeta", 1, ("zeta.erf", "dd", 10), ("more.erf", "ee", 5));

        var plan = KeepOneOfEach.Plan([oldAddIn, newAddIn], _noPicks, Here, []);

        Assert.Equal(["more.erf"], Assert.Single(plan.Sets).Plan.OnlyInRemoved);
    }

    [Fact]
    public void Plan_NoCopies_HasNoSets()
    {
        Assert.Empty(KeepOneOfEach.Plan([_alone], _noPicks, Here, []).Sets);
    }

    private static string Group(LibraryEntry entry) => ModCopies.Find([entry, entry with { Mod = entry.Mod with { Id = "twin" } }])[entry.Mod.Id].Group;

    private static LibraryEntry Entry(string id, string name, int day, params (string Path, string Sha, long Size)[] files) =>
        new(new LibraryMod(id, name, ModKind.Override, [.. files.Select(f => new LibraryModFile(f.Path, f.Size, f.Sha))]), "source", "sha", DateTimeOffset.UnixEpoch.AddDays(day));

    private static LibraryEntry AddIn(string id, int day, params (string Path, string Sha, long Size)[] files) =>
        new(new LibraryMod(id, "Zeta", ModKind.AddIn, [.. files.Select(f => new LibraryModFile(f.Path, f.Size, f.Sha))], "zeta_uid"), "source", "sha", DateTimeOffset.UnixEpoch.AddDays(day));
}
