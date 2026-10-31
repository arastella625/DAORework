using DAORework.Core.Mods.Conflicts;
using DAORework.Core.Mods.Library;
using DAORework.Core.Mods.Planning;
using DAORework.Core.Mods.Safety;
using Xunit;

namespace DAORework.Core.Tests.Mods.Safety;

/// <summary>Program files in a mod are flagged and never deployed (ADR-0018).</summary>
public sealed class ProgramFilesTests
{
    [Theory]
    [InlineData("setup.exe", true)]
    [InlineData("bin/D3D9.DLL", true)]
    [InlineData(@"tools\install.bat", true)]
    [InlineData("readme.js", true)]
    [InlineData("Shortcut.lnk", true)]
    [InlineData("textures/hair_01.dds", false)]
    [InlineData("core/dog.gda", false)]
    [InlineData("module.erf", false)]
    [InlineData("exe", false)]
    [InlineData("notes.exe.txt", false)]
    public void IsProgram_ByExtension(string path, bool program)
    {
        Assert.Equal(program, ProgramFiles.IsProgram(path));
    }

    [Theory]
    [InlineData("hair.dds", new byte[] { (byte)'M', (byte)'Z', 0x90 }, ModFileKind.Program)]
    [InlineData("hair.dds", new byte[] { (byte)'D', (byte)'D', (byte)'S' }, ModFileKind.Data)]
    [InlineData("tool.exe", new byte[] { 0, 0 }, ModFileKind.Program)]
    [InlineData("empty.gda", new byte[0], ModFileKind.Data)]
    [InlineData("m.gda", new byte[] { (byte)'M' }, ModFileKind.Data)]
    public void Classify_ByNameOrExecutableHeader(string path, byte[] start, ModFileKind kind)
    {
        Assert.Equal(kind, ProgramFiles.Classify(path, start));
    }

    [Fact]
    public void Scan_AFileClassifiedAsAProgram_IsNotDeployedWhateverItsName()
    {
        var renamed = new LibraryModFile("hair.dds", 10, "x") { Kind = ModFileKind.Program };
        var mod = new LibraryMod("sneaky", "sneaky", ModKind.Override, [renamed, new LibraryModFile("eyes.dds", 10, "e") { Kind = ModFileKind.Data }]);

        var report = ConflictScanner.Scan([mod], []).Value;

        Assert.Equal(["eyes.dds"], report.Deploy.Select(f => f.FileName));
        Assert.Equal(["hair.dds"], report.ProgramFiles.Select(f => f.FileName));
    }

    [Fact]
    public void Scan_ProgramFiles_AreNotDeployedAndTakeNoPartInOverlaps()
    {
        var report = ConflictScanner.Scan([Mods.Override("a", "hair.dds#1", "d3d9.dll#2"), Mods.Override("b", "d3d9.dll#3")], []).Value;

        Assert.Equal(["hair.dds"], report.Deploy.Select(f => f.FileName));
        Assert.Equal(["d3d9.dll", "d3d9.dll"], report.ProgramFiles.Select(f => f.FileName));
        Assert.Empty(report.OverrideConflicts);
    }

    [Fact]
    public void Plan_ModWithAProgram_CopiesOnlyItsDataAndSaysWhy()
    {
        var plan = Plan(DeploymentRecord.Empty, Mods.Override("reshade-pack", "shaders/a.fx#1", "d3d9.dll#2", "setup.exe#3"));

        Assert.Equal(["packages/core/override/DAORework/reshade-pack/shaders/a.fx"], plan.Operations.Select(o => o.TargetPath));
        var lines = ModActivationPlanText.Render(plan);
        var warning = Assert.Single(lines, l => l.Contains("program", StringComparison.Ordinal));
        Assert.Equal(
            "  ! reshade-pack holds 2 files that are programs (d3d9.dll, setup.exe); programs are never installed into the game's folders. For DXVK, use: fixes apply dxvk <release>.",
            warning);
    }

    /// <summary>A program an older DAORework placed is recorded; with the rule it is no longer wanted, so it is removed.</summary>
    [Fact]
    public void Plan_ProgramPlacedByAnOlderVersion_IsDeleted()
    {
        const string placed = "packages/core/override/DAORework/tool/run.exe";
        var record = new DeploymentRecord(["tool"], [new RecordedFile(placed, "x", "tool")], []);

        var plan = Plan(record, Mods.Override("tool", "run.exe#x"), disk: (placed, "x"));

        var delete = Assert.Single(plan.Operations);
        Assert.Equal(PlanAction.Delete, delete.Action);
        Assert.Equal(placed, delete.TargetPath);
    }

    private static ModActivationPlan Plan(DeploymentRecord record, Core.Mods.Library.LibraryMod mod, params (string Path, string Hash)[] disk) =>
        ModActivationPlanner.Plan(new PlanInput(record, [mod], [], disk.ToDictionary(d => d.Path, d => d.Hash, StringComparer.OrdinalIgnoreCase), [])).Value;
}
