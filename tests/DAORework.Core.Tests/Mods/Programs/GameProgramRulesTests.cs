using DAORework.Core.Diagnostics;
using DAORework.Core.Fixes;
using DAORework.Core.Formats;
using DAORework.Core.Mods.Library;
using DAORework.Core.Mods.Planning;
using DAORework.Core.Mods.Safety;
using Xunit;

namespace DAORework.Core.Tests.Mods.Programs;

/// <summary>P2.8-04 (ADR-0033 §2): what may go into bin_ship, and never over another file.</summary>
public sealed class GameProgramRulesTests
{
    private const string Mod = "luma-0a1b2c3d";

    [Fact]
    public void CheckFiles_A32BitDllAndA64BitOne_PassesNamingThe64BitOne()
    {
        var result = GameProgramRules.CheckFiles("Luma", [Dll("bin_ship/d3d9.dll"), Dll("bin_ship/Luma/upscaler.dll", PortableExecutableMachine.X64), Data("bin_ship/Luma/shader.hlsl")]);

        Assert.Equal(["bin_ship/Luma/upscaler.dll"], result.Value);
    }

    [Fact]
    public void CheckFiles_ADllUnderAnotherName_Passes()
    {
        Assert.True(GameProgramRules.CheckFiles("Luma", [Dll("bin_ship/d3d9.dll"), Dll("bin_ship/Luma-DAO.addon")]).IsSuccess);
    }

    [Fact]
    public void CheckFiles_AProgramThatRunsOnItsOwn_IsRefusedByName()
    {
        var error = GameProgramRules.CheckFiles("Trainer", [Dll("bin_ship/hook.dll"), new GameProgramFileFacts(Target("bin_ship/setup.dat"), true, new PortableExecutableImage(PortableExecutableMachine.X86, IsDll: false))]).Error!;

        Assert.Equal(DiagnosticCodes.ModSafetyProgramRefused, error.Code);
        Assert.Contains("bin_ship/setup.dat is a program that runs on its own", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CheckFiles_AScript_IsRefused()
    {
        var error = GameProgramRules.CheckFiles("Mod", [Dll("bin_ship/a.dll"), Data("bin_ship/install.bat")]).Error!;

        Assert.Contains("bin_ship/install.bat is a script", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CheckFiles_AHeaderThatCannotBeRead_IsRefused()
    {
        var error = GameProgramRules.CheckFiles("Mod", [Dll("bin_ship/a.dll"), new GameProgramFileFacts(Target("bin_ship/b.dll"), true, null)]).Error!;

        Assert.Contains("can't read what kind", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CheckFiles_No32BitDll_IsRefusedSayingTheGameIs32Bit()
    {
        var error = GameProgramRules.CheckFiles("Wrong DXVK", [Dll("bin_ship/d3d9.dll", PortableExecutableMachine.X64)]).Error!;

        Assert.Contains("None of Wrong DXVK's DLLs is 32-bit", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CheckFiles_APathOutsideBinShip_IsRefused()
    {
        var error = GameProgramRules.CheckFiles("Mod", [Dll("bin_ship/../daorigins.dll")]).Error!;

        Assert.Contains("outside the game's bin_ship folder", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CheckTargets_FreePathsOrThisModsOwn_AreFine()
    {
        var state = State(new RecordedFile("bin_ship/d3d9.dll", "aa", Mod), ("bin_ship/d3d9.dll", "aa"));

        Assert.Null(GameProgramRules.CheckTargets(Mod, "Luma", [Target("bin_ship/d3d9.dll"), Target("bin_ship/dxgi.dll")], state, id => id));
    }

    [Fact]
    public void CheckTargets_AFileDaoReworkDidNotPlace_IsTakenAndLeftAlone()
    {
        var error = GameProgramRules.CheckTargets(Mod, "Luma", [Target("bin_ship/d3d9.dll")], State(null, ("bin_ship/d3d9.dll", "bb")), id => id)!;

        Assert.Equal(DiagnosticCodes.ModProgramPathTaken, error.Code);
        Assert.Contains("DAORework didn't put it there", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CheckTargets_AnotherModsFile_NamesItToTurnOffFirst()
    {
        var state = State(new RecordedFile("bin_ship/dinput8.dll", "cc", "controller-11223344"), ("bin_ship/dinput8.dll", "cc"));

        var error = GameProgramRules.CheckTargets("dao-lib-55667788", "DAO Lib", [Target("bin_ship/dinput8.dll")], state, _ => "Native Controller Framework")!;

        Assert.Contains("is Native Controller Framework's, which is on. Turn it off first", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CheckTargets_DxvksFile_PointsToTheFixesPage()
    {
        var state = State(new RecordedFile("bin_ship/d3d9.dll", "dd", FixIds.LibraryId("dxvk-2.3-0011aabb")), ("bin_ship/d3d9.dll", "dd"));

        var error = GameProgramRules.CheckTargets(Mod, "Luma", [Target("bin_ship/d3d9.dll")], state, id => id)!;

        Assert.Contains("is DXVK's, a fix DAORework placed. Turn it off on the Fixes page first", error.Message, StringComparison.Ordinal);
    }

    private static GameProgramFile Target(string path) => new(new LibraryModFile(path, 1, "h"), path);

    private static GameProgramFileFacts Dll(string path, ushort machine = PortableExecutableMachine.X86) =>
        new(Target(path), true, new PortableExecutableImage(machine, IsDll: true));

    private static GameProgramFileFacts Data(string path) => new(Target(path), false, null);

    private static InstallState State(RecordedFile? recorded, (string Path, string Sha) onDisk) =>
        new(
            DeploymentRecord.Empty with { Files = recorded is null ? [] : [recorded], ActiveModIds = recorded is null ? [] : [recorded.ModId] },
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { [onDisk.Path] = onDisk.Sha },
            new HashSet<string>(),
            []);
}
