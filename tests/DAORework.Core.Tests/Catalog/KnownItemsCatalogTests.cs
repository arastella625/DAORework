using DAORework.Core.Catalog;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using Xunit;

namespace DAORework.Core.Tests.Catalog;

public sealed class KnownItemsCatalogTests
{
    private const string Sha = "6a2788e24d900bea771a27353f17e0f88dd96914175c731f385504a3155f2674";

    public static TheoryData<KnownItem, string> BrokenItems => new()
    {
        { Item("a") with { Id = " " }, "an id and a name" },
        { Item("a") with { Name = string.Empty }, "an id and a name" },
        { Item("a") with { Source = "http://example.org/mod" }, "https link" },
        { Item("a") with { Source = "not a link" }, "https link" },
        { Item("a") with { License = " " }, "license" },
        { Item("a") with { Fingerprints = [] }, "at least one fingerprint" },
        { Item("a") with { Fingerprints = [new KnownItemFingerprint((FingerprintKind)42, "x")] }, "no valid kind" },
        { Item("a") with { Fingerprints = [new KnownItemFingerprint(FingerprintKind.AddInUid, " ")] }, "has no value" },
        { Item("a") with { Fingerprints = [new KnownItemFingerprint(FingerprintKind.GameFile, "../outside.dll")] }, "inside the install directory" },
        { Item("a") with { Fingerprints = [new KnownItemFingerprint(FingerprintKind.GameFile, @"C:\Windows\d3d9.dll")] }, "inside the install directory" },
        { Item("a") with { Fingerprints = [new KnownItemFingerprint(FingerprintKind.GameFile, "/etc/d3d9.dll")] }, "inside the install directory" },
        { Item("a") with { Fingerprints = [new KnownItemFingerprint(FingerprintKind.AddInUid, "uid", "DXVK")] }, "only a GameFile" },
        { Item("a") with { Fingerprints = [new KnownItemFingerprint(FingerprintKind.AddInUid, "uid", Version: "1.0")] }, "only a DownloadSha256" },
        { Item("a") with { Fingerprints = [Download(Sha.ToUpperInvariant(), "1.0"), Uid("a")] }, "64 lowercase hex digits" },
        { Item("a") with { Fingerprints = [Download(Sha[..63], "1.0"), Uid("a")] }, "64 lowercase hex digits" },
        { Item("a") with { Fingerprints = [Download(Sha, null), Uid("a")] }, "needs the version" },
        { Item("a") with { Fingerprints = [Download(Sha, "1.0")] }, "a fingerprint found on disk" },
    };

    public static TheoryData<KnownConflict, string> BrokenConflicts => new()
    {
        { Conflict("a", "missing"), "both sides must be catalog entries" },
        { Conflict("a", "A"), "cannot conflict with itself" },
        { Conflict("a", "b") with { Severity = DiagnosticSeverity.Error }, "Info or Warning" },
        { Conflict("a", "b") with { Reason = " " }, "a reason" },
        { Conflict("a", "b") with { Source = "ftp://example.org" }, "https link" },
    };

    public static TheoryData<KnownNeed, string> BrokenNeeds => new()
    {
        { new KnownNeed("missing", "https://example.org/a"), "which is not a catalog entry" },
        { new KnownNeed("A", "https://example.org/a"), "cannot need itself" },
        { new KnownNeed("b", "http://example.org/a"), "must be an https link" },
    };

    [Fact]
    public void Validate_AWellFormedCatalog_Succeeds()
    {
        var catalog = CatalogOf([Item("fixpack"), Item("dialog-patch")], [Conflict("fixpack", "dialog-patch")]);

        Assert.Equal(catalog, KnownItemsCatalog.Validate(catalog).Value);
    }

    [Theory]
    [MemberData(nameof(BrokenNeeds))]
    public void Validate_ABrokenNeed_IsRejected(KnownNeed need, string expected)
    {
        AssertInvalid(KnownItemsCatalog.Validate(CatalogOf([Needing(need), Item("b")], [])), expected);
    }

    [Fact]
    public void Validate_ANeedOfAnotherEntry_IsValid()
    {
        Assert.True(KnownItemsCatalog.Validate(CatalogOf([Needing(new KnownNeed("b", "https://example.org/a")), Item("b")], [])).IsSuccess);
    }

    [Fact]
    public void Validate_NeedsWithoutADownloadHash_AreRejected()
    {
        var item = Item("a") with { Needs = [new KnownNeed("b", "https://example.org/a")] };

        AssertInvalid(KnownItemsCatalog.Validate(CatalogOf([item, Item("b")], [])), "no download hash");
    }

    [Fact]
    public void Validate_MissingLists_BecomeEmpty()
    {
        var catalog = new KnownItemsCatalog(KnownItemsCatalog.CurrentSchemaVersion, null!, null!);

        var validated = KnownItemsCatalog.Validate(catalog).Value;

        Assert.Empty(validated.Items);
        Assert.Empty(validated.Conflicts);
    }

    [Fact]
    public void Validate_Null_Fails()
    {
        AssertInvalid(KnownItemsCatalog.Validate(null), "empty");
    }

    [Fact]
    public void Validate_UnknownSchemaVersion_Fails()
    {
        AssertInvalid(KnownItemsCatalog.Validate(CatalogOf([Item("a")], []) with { SchemaVersion = 2 }), "Schema version 2");
    }

    [Theory]
    [MemberData(nameof(BrokenItems))]
    public void Validate_BrokenItem_FailsNamingTheProblem(KnownItem item, string expected)
    {
        AssertInvalid(KnownItemsCatalog.Validate(CatalogOf([item], [])), expected);
    }

    [Fact]
    public void Validate_LargeAddressAwareFingerprint_NeedsNoValue()
    {
        var item = Item("laa") with { Fingerprints = [new KnownItemFingerprint(FingerprintKind.LargeAddressAware, string.Empty)] };

        Assert.True(KnownItemsCatalog.Validate(CatalogOf([item], [])).IsSuccess);
    }

    [Fact]
    public void Validate_DuplicateIdsIgnoringCase_Fail()
    {
        AssertInvalid(KnownItemsCatalog.Validate(CatalogOf([Item("dxvk"), Item("DXVK")], [])), "appears twice");
    }

    [Theory]
    [MemberData(nameof(BrokenConflicts))]
    public void Validate_BrokenConflict_FailsNamingTheProblem(KnownConflict conflict, string expected)
    {
        AssertInvalid(KnownItemsCatalog.Validate(CatalogOf([Item("a"), Item("b")], [conflict])), expected);
    }

    internal static KnownItem Item(string id) => new(
        id,
        $"Mod {id}",
        KnownItemKind.Mod,
        $"https://example.org/{id}",
        "Permission from the author",
        [new KnownItemFingerprint(FingerprintKind.AddInUid, $"{id}_uid")]);

    [Fact]
    public void Validate_DownloadHashBesideAFingerprintOnDisk_IsValid()
    {
        var item = Item("a") with { Fingerprints = [Uid("a"), Download(Sha, "1.0")] };

        Assert.True(KnownItemsCatalog.Validate(CatalogOf([item], [])).IsSuccess);
    }

    internal static KnownConflict Conflict(string first, string second) =>
        new(first, second, DiagnosticSeverity.Warning, "They edit the same dialogue.", "https://example.org/readme");

    internal static KnownItemsCatalog CatalogOf(IReadOnlyList<KnownItem> items, IReadOnlyList<KnownConflict> conflicts) =>
        new(KnownItemsCatalog.CurrentSchemaVersion, items, conflicts);

    private static KnownItemFingerprint Download(string sha, string? version) => new(FingerprintKind.DownloadSha256, sha, Version: version);

    private static KnownItemFingerprint Uid(string id) => new(FingerprintKind.AddInUid, $"{id}_uid");

    private static KnownItem Needing(KnownNeed need) => Item("a") with { Fingerprints = [Uid("a"), Download(Sha, "1.0")], Needs = [need] };

    private static void AssertInvalid(Result<KnownItemsCatalog> result, string expected)
    {
        Assert.Equal(DiagnosticCodes.CatalogInvalid, result.Error!.Code);
        Assert.Contains(expected, result.Error.Message, StringComparison.Ordinal);
    }
}
