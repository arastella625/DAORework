using DAORework.Core.Mods.Adoption;
using DAORework.Core.Mods.Library;
using Xunit;

namespace DAORework.Core.Tests.Mods.Adoption;

public sealed class AlreadyHeldTests
{
    private const string Uid = "bluehair_uid";

    private static readonly IReadOnlyList<string> _addIn = ["Manifest.xml", $"Contents/addins/{Uid}/module/hair.erf"];

    [Fact]
    public void Find_FilesInTheGame_ComeBeforeTheLibrary()
    {
        var found = Candidate("override:Old Hair", "Old Hair", "packages/core/override/Old Hair/hair.dds");

        var held = AlreadyHeld.Find(["Blue/hair.dds", "readme.txt"], [found], [Override("Blue Hair", "hair.dds")]);

        Assert.Equal("Old Hair", held!.Name);
        Assert.Same(found, held.InGame);
    }

    [Fact]
    public void Find_WhatIsAlreadyTheLibrarysMod_IsTheLibrarys()
    {
        var found = Candidate("override:Old Hair", "Old Hair", "packages/core/override/Old Hair/hair.dds") with { LibraryMatch = new LibraryMatch("id", "Blue Hair", 0) };

        var held = AlreadyHeld.Find(["hair.dds"], [found], [Override("Blue Hair", "hair.dds")]);

        Assert.Equal("Blue Hair", held!.Name);
        Assert.Null(held.InGame);
    }

    [Fact]
    public void Find_AFolderIsNoDifference_AndLetterCaseIsNone()
    {
        var held = AlreadyHeld.Find(["some/folder/HAIR.dds"], [], [Override("Blue Hair", "other/hair.dds")]);

        Assert.Equal("Blue Hair", held!.Name);
    }

    [Fact]
    public void Find_AFileNotHeldAnywhere_MeansItIsADifferentMod()
    {
        Assert.Null(AlreadyHeld.Find(["hair.dds", "new.dds"], [], [Override("Blue Hair", "hair.dds")]));
    }

    [Fact]
    public void Find_TheFileEveryHairModShips_SaysNothing()
    {
        Assert.Null(AlreadyHeld.Find(["chargenmorphcfg.xml"], [], [Override("Blue Hair", "chargenmorphcfg.xml", "hair.dds")]));
    }

    [Fact]
    public void Find_AReadmeAlone_SaysNothing()
    {
        Assert.Null(AlreadyHeld.Find(["readme.txt"], [], [Override("Blue Hair", "readme.txt")]));
    }

    [Fact]
    public void Find_TheClosestOfSeveral_IsNamed()
    {
        var held = AlreadyHeld.Find(["hair.dds"], [], [Override("Big Pack", "hair.dds", "a.dds", "b.dds"), Override("Blue Hair", "hair.dds")]);

        Assert.Equal("Blue Hair", held!.Name);
    }

    [Fact]
    public void Find_AnAddIn_IsMatchedByItsUid_NotItsFileNames()
    {
        var found = Candidate($"addin:{Uid}", "Blue Hair", $"AddIns/{Uid}/module/other.erf") with { Kind = AdoptionCandidateKind.AddIn, Path = $"AddIns/{Uid}" };

        Assert.Same(found, AlreadyHeld.Find(_addIn, [found], [])!.InGame);
        Assert.Equal("Blue Hair", AlreadyHeld.Find(_addIn, [], [new LibraryMod("id", "Blue Hair", ModKind.AddIn, [], Uid)])!.Name);
        Assert.Null(AlreadyHeld.Find(_addIn, [], [new LibraryMod("id", "Red Hair", ModKind.AddIn, [], "other_uid")]));
    }

    [Fact]
    public void Find_ADazipInsideAZip_CantBeToldFromItsNames()
    {
        Assert.Null(AlreadyHeld.Find(["Blue Hair.dazip"], [], [Override("Blue Hair", "Blue Hair.dazip")]));
    }

    private static AdoptionCandidate Candidate(string id, string name, string path) =>
        new(id, AdoptionCandidateKind.OverrideFolder, name, path[..path.LastIndexOf('/')], [new HandInstalledFile(path, 1)]);

    private static LibraryMod Override(string name, params string[] files) =>
        new(name, name, ModKind.Override, [.. files.Select(f => new LibraryModFile(f, 1, "sha"))]);
}
