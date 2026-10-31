using DAORework.Core.Catalog;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using Xunit;
using static DAORework.Core.Tests.Catalog.KnownItemsCatalogTests;

namespace DAORework.Core.Tests.Catalog;

public sealed class KnownItemsScanTests
{
    private readonly KnownItemsCatalog _catalog = CatalogOf(
        [Item("fixpack"), Item("dialog-patch"), Item("auto-loot"), Item("dxvk")],
        [Conflict("fixpack", "dialog-patch"), Conflict("fixpack", "auto-loot")]);

    [Fact]
    public void Create_SortsItemsIntoFoundMissingAndUnresolved()
    {
        var unreadable = new Error(DiagnosticCodes.KnownItemUnreadable, "d3d9.dll could not be read");

        var scan = KnownItemsScan.Create(_catalog, [Found("fixpack")], new Dictionary<string, Error> { ["dxvk"] = unreadable });

        Assert.Equal(["fixpack"], scan.Found.Select(f => f.Item.Id));
        Assert.Equal(["dialog-patch", "auto-loot"], scan.Missing.Select(m => m.Id));
        Assert.Equal([unreadable], scan.Unresolved);
    }

    [Fact]
    public void Create_KeepsOnlyConflictsWithBothSidesFound()
    {
        var scan = KnownItemsScan.Create(_catalog, [Found("fixpack"), Found("DIALOG-PATCH")], new Dictionary<string, Error>());

        var conflict = Assert.Single(scan.Conflicts);
        Assert.Equal("dialog-patch", conflict.Second);
    }

    [Fact]
    public void DetectedKnownItem_TakesTheFirstVersionAnyMatchReported()
    {
        var item = new DetectedKnownItem(Item("dxvk") with { Name = "DXVK" }, [new FingerprintMatch("a"), new FingerprintMatch("b", "v2.3")]);

        Assert.Equal("v2.3", item.Version);
        Assert.Equal("DXVK v2.3", item.DisplayName);
    }

    [Fact]
    public void DetectedKnownItem_WithoutAVersion_IsNamedPlainly()
    {
        Assert.Equal("Mod fixpack", Found("fixpack").DisplayName);
    }

    private DetectedKnownItem Found(string id) =>
        new(_catalog.Items.Single(i => string.Equals(i.Id, id, StringComparison.OrdinalIgnoreCase)), [new FingerprintMatch("somewhere")]);
}
