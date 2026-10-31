using System.IO.Abstractions.TestingHelpers;
using DAORework.Core.Diagnostics;
using DAORework.Infrastructure.Mods;
using Xunit;
using static DAORework.Infrastructure.Tests.Mods.ModScenario;

namespace DAORework.Infrastructure.Tests.Mods;

public sealed class DocumentsScannerTests
{
    private readonly ModScenario _scenario = new();

    [Fact]
    public async Task Scan_HashesThePathsThatExistListsHandPlacedOverridesAndRegisteredAddIns()
    {
        var snapshot = (await new DocumentsScanner(_scenario.FileSystem).ScanAsync(Documents, ["packages/core/override/DAORework/ui/ui.gda", "missing/file.dds"])).Value;

        Assert.Equal(Sha("ui v1"), snapshot.Hashes["packages/core/override/DAORework/ui/ui.gda"]);
        Assert.False(snapshot.Hashes.ContainsKey("missing/file.dds"));
        Assert.Equal(["My Mods/mine.dds"], snapshot.UnmanagedOverridePaths);
        Assert.Equal(["users_own"], snapshot.RegisteredAddInUids);
    }

    [Fact]
    public async Task Scan_FolderTheGameHasNotWrittenYet_IsEmpty()
    {
        var elsewhere = MockUnixSupport.Path(@"C:\Nobody\Documents\BioWare\Dragon Age");

        var snapshot = (await new DocumentsScanner(_scenario.FileSystem).ScanAsync(elsewhere, [])).Value;

        Assert.Empty(snapshot.UnmanagedOverridePaths);
        Assert.Empty(snapshot.RegisteredAddInUids);
    }

    [Fact]
    public async Task Scan_UnreadableAddInsList_Fails()
    {
        _scenario.Write(Documents, "Settings/AddIns.xml", "<AddInsList");

        Assert.Equal(DiagnosticCodes.ModScanFailed, (await new DocumentsScanner(_scenario.FileSystem).ScanAsync(Documents, [])).Error!.Code);
    }

    [Fact]
    public async Task Scan_LockedFile_Fails()
    {
        _scenario.FileSystem.AddFile(_scenario.Full(Documents, "x/locked.dds"), new MockFileData("x") { AllowedFileShare = FileShare.None });

        Assert.Equal(DiagnosticCodes.ModScanFailed, (await new DocumentsScanner(_scenario.FileSystem).ScanAsync(Documents, ["x/locked.dds"])).Error!.Code);
    }
}
