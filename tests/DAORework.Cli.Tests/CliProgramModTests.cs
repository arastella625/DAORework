using System.Buffers.Binary;
using System.IO.Abstractions.TestingHelpers;
using System.IO.Compression;
using DAORework.Cli.Composition;
using DAORework.Core.Mods.Safety;
using Xunit;

namespace DAORework.Cli.Tests;

/// <summary>P2.8-05 (ADR-0033 §3): mods activate and deactivate for a mod that runs inside the game, end to end.</summary>
public sealed class CliProgramModTests : IDisposable
{
    private static readonly string _install = MockUnixSupport.Path(@"D:\Games\Dragon Age");
    private static readonly string _documents = MockUnixSupport.Path(@"C:\Users\Me\Documents\BioWare\Dragon Age");
    private static readonly string _dll = Path.Combine(_install, "bin_ship", "d3d9.dll");

    private readonly CliTestHost _host = new();
    private readonly string _luma;
    private readonly string _hair;

    public CliProgramModTests()
    {
        _host.Scanner.Result = new MalwareScan(MalwareScanStatus.NoThreatFound, "No threats found.");
        _host.FileSystem.AddFile(Path.Combine(_install, "bin_ship", "daorigins.exe"), new MockFileData("exe"));
        _host.FileSystem.AddFile(Path.Combine(_documents, "Settings", "AddIns.xml"), new MockFileData("<AddInsList />"));
        _host.Run("init", "Luma");
        _host.Run("profile", "set-path", "Luma", "install", _install);
        _host.Run("profile", "set-path", "Luma", "documents", _documents);
        _luma = Import("luma.zip", ("Luma/d3d9.dll", Dll()), ("Luma/readme.txt", "read me"u8.ToArray()));
        _hair = Import("hair.zip", ("hair.dds", "dds"u8.ToArray()));
    }

    public void Dispose() => _host.Dispose();

    [Fact]
    public void Activate_WithoutAllowPrograms_RefusesAfterThePlanAndWritesNothing()
    {
        var result = _host.Run("mods", "activate", _luma, "-p", "Luma", "--yes");

        Assert.Equal(ExitCodes.Failure, result.ExitCode);
        Assert.Contains("  Add bin_ship/d3d9.dll", result.Output, StringComparison.Ordinal);
        Assert.Contains(GameProgramTrust.Warning, result.Output, StringComparison.Ordinal);
        Assert.Contains("--allow-programs", result.Output, StringComparison.Ordinal);
        Assert.False(_host.FileSystem.File.Exists(_dll));
    }

    [Fact]
    public void ActivateWithAllowProgramsThenDeactivate_PlacesTheDllAndThenDeletesIt()
    {
        var activated = _host.Run("mods", "activate", _luma, "-p", "Luma", "--allow-programs", _luma, "--yes");

        Assert.Equal(ExitCodes.Success, activated.ExitCode);
        Assert.Contains("Undo with: fixes rollback", activated.Output, StringComparison.Ordinal);
        Assert.Equal(Dll(), _host.FileSystem.File.ReadAllBytes(_dll));
        Assert.Contains("No fixes are applied", _host.Run("fixes", "status", "-p", "Luma").Output, StringComparison.Ordinal);

        var deactivated = _host.Run("mods", "deactivate", _luma, "-p", "Luma", "--yes");

        Assert.Equal(ExitCodes.Success, deactivated.ExitCode);
        Assert.False(_host.FileSystem.File.Exists(_dll));
    }

    [Fact]
    public void Activate_DryRun_ShowsThePlanWithoutAskingForTheYes()
    {
        var result = _host.Run("mods", "activate", _luma, "-p", "Luma", "--dry-run");

        Assert.Equal(ExitCodes.Success, result.ExitCode);
        Assert.Contains("Dry run: nothing was changed.", result.Output, StringComparison.Ordinal);
        Assert.False(_host.FileSystem.File.Exists(_dll));
    }

    [Fact]
    public void Activate_WithAnotherMod_ChangesNothingAndSaysHow()
    {
        var result = _host.Run("mods", "activate", _hair, _luma, "-p", "Luma", "--allow-programs", _luma, "--yes");

        Assert.Equal(ExitCodes.Failure, result.ExitCode);
        Assert.Contains($"Run: mods activate {_luma} --allow-programs {_luma}", result.Output, StringComparison.Ordinal);
        Assert.False(_host.FileSystem.File.Exists(_dll));
        Assert.Contains("No mods are active", _host.Run("mods", "status", "-p", "Luma").Output, StringComparison.Ordinal);
    }

    [Fact]
    public void Activate_NotScanned_IsRefused()
    {
        _host.Scanner.Result = new MalwareScan(MalwareScanStatus.NotScanned, "No antivirus answered.");

        var result = _host.Run("mods", "activate", _luma, "-p", "Luma", "--allow-programs", _luma, "--yes");

        Assert.Equal(ExitCodes.Failure, result.ExitCode);
        Assert.Contains("Cannot plan the change: luma runs its own program", result.Output, StringComparison.Ordinal);
        Assert.False(_host.FileSystem.File.Exists(_dll));
    }

    [Fact]
    public void Deactivate_WithoutAnInstallFolder_GoesTheUsualWay()
    {
        _host.Run("init", "NoInstall");
        _host.Run("profile", "set-path", "NoInstall", "documents", _documents);

        var result = _host.Run("mods", "deactivate", _luma, "-p", "NoInstall", "--yes");

        Assert.DoesNotContain("has no install folder", result.Output, StringComparison.Ordinal);
    }

    /// <summary>A made-up 32-bit DLL header.</summary>
    private static byte[] Dll()
    {
        var header = new byte[256];
        "MZ"u8.CopyTo(header);
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(0x3C), 0x80);
        "PE\0\0"u8.CopyTo(header.AsSpan(0x80));
        BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(0x84), 0x014C);
        BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(0x84 + 18), 0x2102);
        return header;
    }

    private string Import(string name, params (string Path, byte[] Bytes)[] files)
    {
        var path = Path.Combine(MockUnixSupport.Path(@"C:\Downloads"), name);
        using var buffer = new MemoryStream();
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (entry, bytes) in files)
            {
                using var stream = zip.CreateEntry(entry).Open();
                stream.Write(bytes);
            }
        }

        _host.FileSystem.AddFile(path, new MockFileData(buffer.ToArray()));
        var output = _host.Run("mods", "import", path).Output;
        return output[(output.IndexOf(" as ", StringComparison.Ordinal) + 4)..output.IndexOf(',', StringComparison.Ordinal)];
    }
}
