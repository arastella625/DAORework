using System.Text;
using DAORework.Core.Catalog;
using DAORework.Core.Diagnostics;
using DAORework.Infrastructure.Catalog;
using Xunit;

namespace DAORework.Infrastructure.Tests.Catalog;

/// <summary>The catalog that ships: a bad entry in catalog/known-items.json fails here, not in a user's diagnostics (ADR-0011).</summary>
public sealed class EmbeddedKnownItemsCatalogSourceTests
{
    [Fact]
    public void Load_TheShippedCatalog_IsValid()
    {
        var result = new EmbeddedKnownItemsCatalogSource().Load();

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.NotEmpty(result.Value.Items);
    }

    [Fact]
    public void Load_TheShippedCatalog_RecognizesDxvkByItsVersionMarker()
    {
        var dxvk = Assert.Single(new EmbeddedKnownItemsCatalogSource().Load().Value.Items, i => i.Id == "dxvk");

        var fingerprint = Assert.Single(dxvk.Fingerprints);
        Assert.Equal(FingerprintKind.GameFile, fingerprint.Kind);
        Assert.Equal("bin_ship/d3d9.dll", fingerprint.Value);
        Assert.Equal("DXVK: ", fingerprint.Marker);
        Assert.NotNull(dxvk.WhenMissing);
    }

    [Fact]
    public void Load_TheShippedCatalog_CarriesTheLargeAddressAwareEntryWithoutDuplicatingTheLaaWarning()
    {
        var laa = Assert.Single(new EmbeddedKnownItemsCatalogSource().Load().Value.Items, i => i.Id == "large-address-aware");

        Assert.Equal(FingerprintKind.LargeAddressAware, Assert.Single(laa.Fingerprints).Kind);

        // DAO_LAA_DISABLED already warns when the flag is missing; a suggestion here would say it twice.
        Assert.Null(laa.WhenMissing);
    }

    /// <summary>The UIDs read out of the mods' own .dazip manifests (catalog/README.md lists the downloads and their hashes).</summary>
    [Theory]
    [InlineData("qwinn-fixpack", "qwinn_fixpack_3")]
    [InlineData("extra-dog-slot", "Extra_Dog_Slot")]
    public void Load_TheShippedCatalog_RecognizesAddInsByTheirManifestUid(string id, string uid)
    {
        var item = Assert.Single(new EmbeddedKnownItemsCatalogSource().Load().Value.Items, i => i.Id == id);

        var fingerprint = Assert.Single(item.Fingerprints, f => f.Kind == FingerprintKind.AddInUid);
        Assert.Equal(uid, fingerprint.Value);
    }

    /// <summary>The downloads' hashes as catalog/README.md records them (ADR-0018).</summary>
    [Theory]
    [InlineData("qwinn-fixpack", "6a2788e24d900bea771a27353f17e0f88dd96914175c731f385504a3155f2674", "3.5")]
    [InlineData("extra-dog-slot", "973f9879f5301581adb922c14c89581d97c3a30b8b90151a3e9cfa4e784cb8d2", "1.41")]
    public void Load_TheShippedCatalog_KnowsTheOfficialDownloads(string id, string sha256, string version)
    {
        var item = Assert.Single(new EmbeddedKnownItemsCatalogSource().Load().Value.Items, i => i.Id == id);

        Assert.Contains(new KnownItemFingerprint(FingerprintKind.DownloadSha256, sha256, Version: version), item.Fingerprints);
    }

    /// <summary>
    /// Every official add-in in the Settings\AddIns.xml of a Steam Ultimate Edition install (catalog/README.md), so
    /// adoption never offers one (ADR-0015).
    /// </summary>
    [Theory]
    [InlineData("dlc-awakening", "DAO_PRC_EP_1")]
    [InlineData("dlc-return-to-ostagar", "DAO_PRC_CP_1")]
    [InlineData("dlc-the-stone-prisoner", "DAO_PRC_CP_2")]
    [InlineData("dlc-feastday-gifts", "DAO_PRC_CP_3")]
    [InlineData("dlc-feastday-pranks", "DAO_PRC_CP_7")]
    [InlineData("dlc-the-darkspawn-chronicles", "DAO_PRC_DRK")]
    [InlineData("dlc-the-darkspawn-chronicles", "DAO_PRC_DRK_DAO")]
    [InlineData("dlc-the-darkspawn-chronicles", "DAO_PRC_DRK_EP1")]
    [InlineData("dlc-the-golems-of-amgarrak", "DAO_PRC_GIB")]
    [InlineData("dlc-the-golems-of-amgarrak", "DAO_PRC_GIB_DAO")]
    [InlineData("dlc-the-golems-of-amgarrak", "DAO_PRC_GIB_EP1")]
    [InlineData("dlc-lelianas-song", "DAO_PRC_LEL")]
    [InlineData("dlc-lelianas-song", "DAO_PRC_LEL_DAO")]
    [InlineData("dlc-lelianas-song", "DAO_PRC_LEL_EP1")]
    [InlineData("dlc-wardens-keep", "DAO_PRC_NRX_1")]
    [InlineData("dlc-blood-dragon-armor", "DAO_PRC_PROMO_ME")]
    [InlineData("dlc-witch-hunt", "DAO_PRC_STR")]
    [InlineData("dlc-witch-hunt", "DAO_PRC_STR_DAO")]
    [InlineData("dlc-witch-hunt", "DAO_PRC_STR_EP1")]
    public void Load_TheShippedCatalog_MarksEveryUltimateEditionAddInOfficial(string id, string uid)
    {
        var item = Assert.Single(new EmbeddedKnownItemsCatalogSource().Load().Value.Items, i => i.Id == id);

        Assert.Equal(KnownItemKind.Official, item.Kind);
        Assert.Contains(new KnownItemFingerprint(FingerprintKind.AddInUid, uid), item.Fingerprints);
    }

    [Fact]
    public void Load_TheShippedCatalog_OfficialEntriesHaveOnlyAddInUidFingerprintsAndNoAdvice()
    {
        var official = new EmbeddedKnownItemsCatalogSource().Load().Value.Items.Where(i => i.Kind == KnownItemKind.Official).ToList();

        Assert.Equal(11, official.Count);
        Assert.All(official, item => Assert.All(item.Fingerprints, f => Assert.Equal(FingerprintKind.AddInUid, f.Kind)));
        Assert.All(official, item => Assert.Null(item.WhenMissing));
    }

    [Fact]
    public void Parse_CamelCaseDocumentWithOptionalFieldsLeftOut_Loads()
    {
        const string json = """
            { "schemaVersion": 1, "items": [ { "id": "a", "name": "A", "kind": "Mod", "source": "https://example.org/a",
              "license": "MIT", "fingerprints": [ { "kind": "AddInUid", "value": "a_uid" } ] } ] }
            """;

        var catalog = Parse(json).Value;

        var item = Assert.Single(catalog.Items);
        Assert.Null(item.WhenMissing);
        Assert.Null(item.Fingerprints[0].Marker);
        Assert.Empty(catalog.Conflicts);
    }

    [Theory]
    [InlineData("{ not json")]
    [InlineData("""{ "schemaVersion": 1, "items": [ { "id": "a", "kind": "Sandwich" } ] }""")]
    [InlineData("""{ "schemaVersion": 1, "items": [ { "id": "a", "name": "A", "kind": "Mod", "source": "https://example.org", "license": "MIT", "fingerprints": [] } ] }""")]
    [InlineData("null")]
    public void Parse_BrokenDocument_FailsWithTheCatalogCode(string json)
    {
        Assert.Equal(DiagnosticCodes.CatalogInvalid, Parse(json).Error!.Code);
    }

    private static Core.Common.Result<KnownItemsCatalog> Parse(string json)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        return EmbeddedKnownItemsCatalogSource.Parse(stream);
    }
}
