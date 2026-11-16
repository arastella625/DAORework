using DAORework.Core.Catalog;
using DAORework.Core.Fixes;
using Xunit;

namespace DAORework.Core.Tests.Catalog;

/// <summary>P2.8-06 (ADR-0033 §5): what a mod needs that isn't in the game, as its preview warns.</summary>
public sealed class NeedsCheckTests
{
    private static readonly KnownItem _lib = KnownItemsCatalogTests.Item("lib") with { Name = "DAO Lib" };
    private static readonly KnownItem _laa = KnownItemsCatalogTests.Item(FixIds.LargeAddressAware) with { Name = "4 GB patch" };
    private static readonly KnownItem _dxvk = KnownItemsCatalogTests.Item(FixIds.Dxvk) with { Name = "DXVK", Kind = KnownItemKind.Fix };
    private static readonly KnownItem _mod = KnownItemsCatalogTests.Item("plugin") with
    {
        Needs = [new KnownNeed("lib", "https://example.org/plugin"), new KnownNeed(FixIds.LargeAddressAware, "https://example.org/plugin"), new KnownNeed(FixIds.Dxvk, "https://example.org/plugin")],
    };

    private static readonly KnownItemsCatalog _catalog = KnownItemsCatalogTests.CatalogOf([_lib, _laa, _dxvk, _mod], []);

    [Fact]
    public void Warnings_AMissingFix_SaysToApplyItOnTheFixesTab()
    {
        var warning = Assert.Single(NeedsCheck.Warnings(_catalog, _mod, "Plugin", new HashSet<string> { FixIds.Dxvk }, steamProtected: false));

        Assert.Equal("Plugin needs DXVK, its author says, and it isn't in your game. Apply it first on the Fixes tab.", warning);
    }

    [Fact]
    public void Warnings_NothingMissing_SaysNothing()
    {
        Assert.Empty(NeedsCheck.Warnings(_catalog, _mod, "Plugin", new HashSet<string>(), steamProtected: false));
    }

    [Fact]
    public void Warnings_AMissingMod_NamesItAndSaysToTurnItOnFirst()
    {
        var warning = Assert.Single(NeedsCheck.Warnings(_catalog, _mod, "Plugin", new HashSet<string> { "lib" }, steamProtected: false));

        Assert.Equal("Plugin needs DAO Lib, its author says, and it isn't in your game. Turn it on first.", warning);
    }

    [Fact]
    public void Warnings_NoLargeAddressAwarePatch_SaysWhereToApplyIt()
    {
        var warning = Assert.Single(NeedsCheck.Warnings(_catalog, _mod, "Plugin", new HashSet<string> { FixIds.LargeAddressAware }, steamProtected: false));

        Assert.EndsWith("Apply it first on the Fixes tab (fixes apply laa).", warning, StringComparison.Ordinal);
    }

    [Fact]
    public void Warnings_OnSteamsProtectedExecutable_SaysThePatchCantGoOnAndPointsNowhereElse()
    {
        var warning = Assert.Single(NeedsCheck.Warnings(_catalog, _mod, "Plugin", new HashSet<string> { FixIds.LargeAddressAware }, steamProtected: true));

        Assert.Equal("Plugin needs the 4 GB patch, its author says, and may crash without it. Your daorigins.exe is Steam's protected copy, which can't take the patch.", warning);
    }
}
