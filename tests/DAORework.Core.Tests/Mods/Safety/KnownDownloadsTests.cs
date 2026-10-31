using DAORework.Core.Catalog;
using DAORework.Core.Mods.Library;
using DAORework.Core.Mods.Safety;
using DAORework.Core.Tests.Catalog;
using Xunit;

namespace DAORework.Core.Tests.Mods.Safety;

/// <summary>A download is compared with the catalog's known downloads of the item it is (ADR-0018).</summary>
public sealed class KnownDownloadsTests
{
    private const string Known = "6a2788e24d900bea771a27353f17e0f88dd96914175c731f385504a3155f2674";
    private const string Other = "0000000000000000000000000000000000000000000000000000000000000000";

    private static readonly KnownItem _fixpack = KnownItemsCatalogTests.Item("fixpack") with
    {
        Fingerprints = [new(FingerprintKind.AddInUid, "fixpack_uid"), new(FingerprintKind.DownloadSha256, Known, Version: "3.5")],
    };

    private static readonly KnownItem _patch = KnownItemsCatalogTests.Item("patch") with
    {
        Fingerprints = [new(FingerprintKind.OverrideFile, "patch.dlg"), new(FingerprintKind.DownloadSha256, Known.Replace('6', '7'), Version: "2")],
    };

    private static readonly KnownItemsCatalog _catalog = KnownItemsCatalogTests.CatalogOf([_fixpack, _patch, KnownItemsCatalogTests.Item("plain")], []);

    [Fact]
    public void Check_TheKnownDownload_MatchesWithItsVersion()
    {
        var check = KnownDownloads.Check(_catalog, Entry(Mods.Override("anything", "a.dds"), "Fixpack.zip", Known.ToUpperInvariant()));

        Assert.Equal(new DownloadCheck(DownloadCheckKind.Matches, _fixpack, "3.5"), check);
    }

    [Fact]
    public void Check_AnAddInTheCatalogKnowsByItsUid_FromAnotherDownload_Differs()
    {
        var check = KnownDownloads.Check(_catalog, Entry(Mods.AddIn("fixpack", "FIXPACK_UID", "Manifest.xml"), "Fixpack (reupload).zip", Other));

        Assert.Equal(new DownloadCheck(DownloadCheckKind.Differs, _fixpack), check);
    }

    [Fact]
    public void Check_AnOverrideModTheCatalogKnowsByAFileName_FromAnotherDownload_Differs()
    {
        var check = KnownDownloads.Check(_catalog, Entry(Mods.Override("patch", "override/dialog/patch.dlg"), "patch.7z", Other));

        Assert.Equal(new DownloadCheck(DownloadCheckKind.Differs, _patch), check);
    }

    [Fact]
    public void Check_AKnownModImportedFromAFolder_CannotBeCompared()
    {
        var check = KnownDownloads.Check(_catalog, Entry(Mods.AddIn("fixpack", "fixpack_uid", "Manifest.xml"), "Fixpack", Other));

        Assert.Equal(new DownloadCheck(DownloadCheckKind.FromAFolder, _fixpack), check);
    }

    [Fact]
    public void Check_AnItemWithNoKnownDownloads_IsUnknown()
    {
        var check = KnownDownloads.Check(_catalog, Entry(Mods.AddIn("plain", "plain_uid", "Manifest.xml"), "plain.dazip", Other));

        Assert.Equal(DownloadCheck.Unknown, check);
    }

    [Fact]
    public void Check_AnAdoptedMod_IsNeverComparedWithADownload()
    {
        var entry = Entry(Mods.AddIn("fixpack", "fixpack_uid", "Manifest.xml"), "fixpack_uid", Other) with
        {
            Origin = new ModOrigin(ModOriginKind.Adopted, "AddIns/fixpack_uid"),
        };

        Assert.Equal(DownloadCheck.Unknown, KnownDownloads.Check(_catalog, entry));
    }

    private static LibraryEntry Entry(LibraryMod mod, string sourceName, string sha256) =>
        new(mod, sourceName, sha256, DateTimeOffset.UnixEpoch);
}
