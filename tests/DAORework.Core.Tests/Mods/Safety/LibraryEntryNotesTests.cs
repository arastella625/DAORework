using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Library;
using DAORework.Core.Mods.Safety;
using DAORework.Core.Tests.Catalog;
using Xunit;

namespace DAORework.Core.Tests.Mods.Safety;

/// <summary>What the CLI and the window say about a library mod's safety (ADR-0018), worded in one place.</summary>
public sealed class LibraryEntryNotesTests
{
    private static readonly LibraryEntry _entry = new(Mods.Override("hair", "hair.dds", "setup.exe"), "Hair.zip", "abc", DateTimeOffset.UnixEpoch);

    [Fact]
    public void Scan_Clean_NamesTheAntivirusAndNeverSaysSafe()
    {
        var entry = _entry with { Scan = new MalwareScan(MalwareScanStatus.NoThreatFound, "No threats found by Defender.") };

        Assert.Equal("Antivirus: No threats found by Defender.", LibraryEntryNotes.Scan(entry));
        Assert.Equal("Not scanned: Imported before DAORework scanned mods with the antivirus.", LibraryEntryNotes.Scan(_entry));
    }

    [Fact]
    public void Programs_NamesThemOrSaysNothing()
    {
        var withKinds = _entry with { Mod = _entry.Mod with { Files = [.. _entry.Mod.Files.Select(f => f with { Kind = f.RelativePath.EndsWith(".exe", StringComparison.Ordinal) ? ModFileKind.Program : ModFileKind.Data })] } };
        var dataOnly = _entry with { Mod = Mods.Override("hair", "hair.dds") };

        Assert.Equal("It holds 1 program file(s) (setup.exe). They stay in the library and are never installed into the game's folders.", LibraryEntryNotes.Programs(withKinds));
        Assert.Null(LibraryEntryNotes.Programs(dataOnly));
    }

    [Fact]
    public void Download_EachOutcome_IsWordedOrLeftOut()
    {
        var item = KnownItemsCatalogTests.Item("dog");

        Assert.Equal("Matches the known download of Mod dog 1.41.", LibraryEntryNotes.Download(new DownloadCheck(DownloadCheckKind.Matches, item, "1.41")));
        Assert.StartsWith("It was imported from a folder, so it cannot be compared with the known downloads of Mod dog.", LibraryEntryNotes.Download(new DownloadCheck(DownloadCheckKind.FromAFolder, item)), StringComparison.Ordinal);
        Assert.Null(LibraryEntryNotes.Download(new DownloadCheck(DownloadCheckKind.Differs, item)));
        Assert.Null(LibraryEntryNotes.Download(DownloadCheck.Unknown));
    }

    [Fact]
    public void DownloadWarning_OnlyForADifferingDownload()
    {
        var item = KnownItemsCatalogTests.Item("dog");

        var warning = LibraryEntryNotes.DownloadWarning(_entry, new DownloadCheck(DownloadCheckKind.Differs, item));

        Assert.Equal(new DiagnosticIssue(DiagnosticSeverity.Warning, DiagnosticCodes.ModSafetyDownloadDiffers, "Hair.zip differs from the known downloads of Mod dog.", "Unless it is a newer version, get it from https://example.org/dog"), warning);
        Assert.Null(LibraryEntryNotes.DownloadWarning(_entry, new DownloadCheck(DownloadCheckKind.Matches, item, "1")));
    }
}
