using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using DAORework.Application.Abstractions;
using DAORework.Application.Fixes;
using DAORework.Application.Mods.Apply;
using DAORework.Application.Mods.Backups;
using DAORework.Application.Mods.Programs;
using DAORework.Application.Tests.Fakes;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Fixes;
using DAORework.Core.Mods.Library;
using DAORework.Core.Mods.Planning;
using DAORework.Core.Mods.Safety;
using Xunit;

namespace DAORework.Application.Tests.Mods.Programs;

/// <summary>P2.8-04 (ADR-0033): planning and applying a mod that runs inside the game, on the fix path's record.</summary>
public sealed class GameProgramModTests
{
    private const string Install = "D:/Games/Dragon Age";
    private const string Luma = "luma-0a1b2c3d";

    private static readonly byte[] _x86 = Pe(0x014C, dll: true);
    private static readonly byte[] _x64 = Pe(0x8664, dll: true);

    private readonly InMemoryDeploymentStores _stores = new();
    private readonly FakeDeploymentBackupStore _backups = new();
    private readonly FakeInstallFiles _files = new();
    private readonly FakeLibraryFileReader _reader = new();
    private readonly FakeMalwareScanner _scanner = new();
    private readonly ProgramLibrary _library = new();

    [Fact]
    public async Task PlanOn_ModWithItsDllsAtTheRoot_CopiesEverythingIntoBinShipAndScansThePrograms()
    {
        Add(Luma, ModKind.GameProgram, ("Luma/d3d9.dll", _x86), ("Luma/Luma/upscaler.dll", _x64), ("Luma/Luma/tonemap.hlsl", Text("shader")));

        var planned = (await PlanOn(Luma)).Value;

        Assert.Equal(["bin_ship/d3d9.dll", "bin_ship/Luma/upscaler.dll", "bin_ship/Luma/tonemap.hlsl"], planned.Plan.Change.Operations.Select(o => o.TargetPath));
        Assert.All(planned.Plan.Change.Operations, o => Assert.Equal(PlanAction.Copy, o.Action));
        Assert.Equal(["bin_ship/d3d9.dll", "bin_ship/Luma/upscaler.dll"], _scanner.Scanned.Select(s => s.Name));
        Assert.Contains(planned.Notes, n => n.StartsWith("64-bit, so the 32-bit game", StringComparison.Ordinal) && n.Contains("bin_ship/Luma/upscaler.dll", StringComparison.Ordinal));
    }

    [Fact]
    public async Task PlanOn_ModWithABinShipFolder_LeavesTheReadmeInTheLibrary()
    {
        Add("dao-lib-11223344", ModKind.GameProgram, ("DAO Lib/bin_ship/dinput8.dll", _x86), ("DAO Lib/readme.txt", Text("read me")));

        var planned = (await PlanOn("dao-lib-11223344")).Value;

        Assert.Equal("bin_ship/dinput8.dll", Assert.Single(planned.Plan.Change.Operations).TargetPath);
        Assert.Contains(planned.Notes, n => n.StartsWith("1 file outside its bin_ship folder", StringComparison.Ordinal));
    }

    [Fact]
    public async Task PlanOn_AProgramThatRunsOnItsOwn_IsRefused()
    {
        Add(Luma, ModKind.GameProgram, ("d3d9.dll", _x86), ("launcher.dat", Pe(0x014C, dll: false)));

        Assert.Equal(DiagnosticCodes.ModSafetyProgramRefused, (await PlanOn(Luma)).Error!.Code);
    }

    [Theory]
    [InlineData(MalwareScanStatus.NotScanned, DiagnosticCodes.ModSafetyProgramNotScanned)]
    [InlineData(MalwareScanStatus.ThreatFound, DiagnosticCodes.ModSafetyThreatFound)]
    public async Task PlanOn_AScanThatIsNotClean_IsRefused(MalwareScanStatus status, string code)
    {
        Add(Luma, ModKind.GameProgram, ("d3d9.dll", _x86));
        _scanner.Result = new MalwareScan(status, "no antivirus answered");

        Assert.Equal(code, (await PlanOn(Luma)).Error!.Code);
    }

    [Fact]
    public async Task PlanOn_ALibraryCopyThatChanged_IsRefused()
    {
        Add(Luma, ModKind.GameProgram, ("d3d9.dll", _x86));
        _reader.Files[$"{Luma}/d3d9.dll"] = _x64;

        var error = (await PlanOn(Luma)).Error!;

        Assert.Equal(DiagnosticCodes.ModSafetyProgramRefused, error.Code);
        Assert.Contains("has changed since", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PlanOn_AFileAlreadyInTheGameFolder_IsTakenAndNothingIsPlanned()
    {
        Add(Luma, ModKind.GameProgram, ("d3d9.dll", _x86));
        _files.Files["bin_ship/d3d9.dll"] = Text("someone else's");

        Assert.Equal(DiagnosticCodes.ModProgramPathTaken, (await PlanOn(Luma)).Error!.Code);
        Assert.Empty(_scanner.Scanned);
    }

    [Fact]
    public async Task PlanOn_AlreadyOnWithAnEditedFile_SaysToTurnItOffFirst()
    {
        Add(Luma, ModKind.GameProgram, ("d3d9.dll", _x86), ("settings.ini", Text("scale=1")));
        await Apply((await PlanOn(Luma)).Value, accepted: true);
        _files.Files["bin_ship/d3d9.dll"] = _x86;
        _files.Files["bin_ship/settings.ini"] = Text("scale=2");

        var error = (await PlanOn(Luma)).Error!;

        Assert.Equal(DiagnosticCodes.ModProgramPathTaken, error.Code);
        Assert.Contains("bin_ship/settings.ini has changed since luma placed it", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PlanOn_AnOverrideMod_IsRefused()
    {
        Add("hair-99887766", ModKind.Override, ("hair.dds", Text("dds")));

        Assert.Equal(DiagnosticCodes.ModSafetyProgramRefused, (await PlanOn("hair-99887766")).Error!.Code);
    }

    [Fact]
    public async Task Apply_WithoutThePlayersYes_WritesNothing()
    {
        Add(Luma, ModKind.GameProgram, ("d3d9.dll", _x86));

        var result = await Apply((await PlanOn(Luma)).Value, accepted: false);

        Assert.Equal(DiagnosticCodes.ModProgramNotAccepted, result.Error!.Code);
        Assert.Empty(_stores.Written);
        Assert.Null(_stores.Saved);
    }

    [Fact]
    public async Task Apply_WithThePlayersYes_PlacesTheFilesAndFixStatusStillListsOnlyFixes()
    {
        Add(Luma, ModKind.GameProgram, ("d3d9.dll", _x86));

        Assert.True((await Apply((await PlanOn(Luma)).Value, accepted: true)).IsSuccess);

        Assert.Equal(["Copy bin_ship/d3d9.dll"], _stores.Written);
        Assert.Equal([Luma], _stores.Saved!.ActiveModIds);
        Assert.Empty((await new GetFixStatus(_stores, _stores, _backups, new FakeFixLibrary(), _files).ExecuteAsync(Install)).Value.Applied);
    }

    [Fact]
    public async Task PlanOff_DeletesItsFilesAndLeavesAnEditedOneNamed()
    {
        Add(Luma, ModKind.GameProgram, ("d3d9.dll", _x86), ("settings.ini", Text("scale=1")));
        await Apply((await PlanOn(Luma)).Value, accepted: true);
        _files.Files["bin_ship/d3d9.dll"] = _x86;
        _files.Files["bin_ship/settings.ini"] = Text("scale=2");

        var planned = (await Plan(Luma, FixChangeKind.Remove)).Value;

        var delete = Assert.Single(planned.Plan.Change.Operations);
        Assert.Equal((PlanAction.Delete, "bin_ship/d3d9.dll"), (delete.Action, delete.TargetPath));
        Assert.Equal(["bin_ship/settings.ini"], planned.Plan.ChangedSince);
        Assert.Empty(planned.Plan.Change.NextRecord.ActiveModIds);
    }

    [Fact]
    public async Task PlanOff_AModThatIsNotOn_SaysSo()
    {
        Assert.Equal(DiagnosticCodes.ModNotActive, (await Plan(Luma, FixChangeKind.Remove)).Error!.Code);
    }

    private static byte[] Text(string text) => Encoding.UTF8.GetBytes(text);

    private static byte[] Pe(ushort machine, bool dll)
    {
        var header = new byte[256];
        "MZ"u8.CopyTo(header);
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(0x3C), 0x80);
        "PE\0\0"u8.CopyTo(header.AsSpan(0x80));
        BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(0x84), machine);
        BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(0x84 + 18), dll ? (ushort)0x2102 : (ushort)0x0102);
        return header;
    }

    private void Add(string id, ModKind kind, params (string Path, byte[] Bytes)[] files)
    {
        var mod = new LibraryMod(id, id.Split('-')[0], kind, [.. files.Select(f => new LibraryModFile(f.Path, f.Bytes.Length, Convert.ToHexStringLower(SHA256.HashData(f.Bytes))))]);
        _library.Entries.Add(new LibraryEntry(mod, $"{id}.zip", "abc", DateTimeOffset.UnixEpoch));
        foreach (var (path, bytes) in files)
        {
            _reader.Files[$"{id}/{path}"] = bytes;
        }
    }

    private Task<Result<PlannedGameProgramMod>> PlanOn(string modId) => Plan(modId, FixChangeKind.Apply);

    private Task<Result<PlannedGameProgramMod>> Plan(string modId, FixChangeKind kind) =>
        new PlanGameProgramMod(_stores, _stores, _library, new ReadInstallState(_files), new ReadGameProgramFiles(_reader, _scanner))
            .ExecuteAsync(new PlanGameProgramModRequest(Install, modId, kind));

    private Task<Result<FixApplyOutcome>> Apply(PlannedGameProgramMod planned, bool accepted) =>
        new ApplyGameProgramMod(new ApplyFix(new FakeFixLibrary(), new ApplyModPlan(new BackUpDeployment(_backups), _stores, new JournaledApplyRunner(_stores, _stores, _stores)), _stores))
            .ExecuteAsync(new ApplyGameProgramModRequest(Install, planned, accepted));

    private sealed class ProgramLibrary : IModLibrary
    {
        public List<LibraryEntry> Entries { get; } = [];

        public Task<Result<LibraryImport>> ImportAsync(string sourcePath, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<LibraryEntry>> ListAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<LibraryEntry>>(Entries);

        public Task<Result<string>> ReadTextAsync(string modId, string relativePath, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<Result<LibraryModFile>> StoreCombinedAsync(string fileName, string text, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
