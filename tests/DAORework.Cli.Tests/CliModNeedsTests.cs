using System.Buffers.Binary;
using System.IO.Abstractions.TestingHelpers;
using System.IO.Compression;
using DAORework.Cli.Composition;
using DAORework.Core.Common;
using DAORework.Core.Mods.Safety;
using Xunit;

namespace DAORework.Cli.Tests;

/// <summary>
/// P2.8-06 (ADR-0033 §5): turning on a mod the catalog says needs something warns when it is missing. Made-up files
/// shaped like the Native Controller Framework's download, which needs the 4 GB patch; nothing of the real mod's.
/// </summary>
public sealed class CliModNeedsTests : IDisposable
{
    private const string Warning = "controller needs the 4 GB patch, its author says";

    private static readonly string _install = MockUnixSupport.Path(@"D:\Games\Dragon Age");
    private static readonly string _documents = MockUnixSupport.Path(@"C:\Users\Me\Documents\BioWare\Dragon Age");

    private readonly CliTestHost _host = new();
    private string _controller = string.Empty;

    public CliModNeedsTests()
    {
        _host.Scanner.Result = new MalwareScan(MalwareScanStatus.NoThreatFound, "No threats found.");
        _host.FileSystem.AddFile(Path.Combine(_documents, "Settings", "AddIns.xml"), new MockFileData("<AddInsList />"));
    }

    public void Dispose() => _host.Dispose();

    [Fact]
    public void WithoutThe4GbPatch_WarnsAndSaysWhereToApplyIt()
    {
        Setup(Executable(largeAddressAware: false, steamProtected: false));

        var result = _host.Run("mods", "activate", _controller, "-p", "Pad", "--dry-run");

        Assert.Equal(ExitCodes.Success, result.ExitCode);
        Assert.Contains($"Warning: {Warning}. Apply it first on the Fixes tab (fixes apply laa).", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void OnSteamsProtectedExecutable_SaysThePatchCantGoOnAndStopsThere()
    {
        Setup(Executable(largeAddressAware: false, steamProtected: true));

        var result = _host.Run("mods", "activate", _controller, "-p", "Pad", "--dry-run");

        Assert.Contains($"Warning: {Warning}, and may crash without it. Your daorigins.exe is Steam's protected copy, which can't take the patch.", result.Output, StringComparison.Ordinal);
        Assert.DoesNotContain("fixes apply laa", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void WithThe4GbPatch_SaysNothingAboutIt()
    {
        Setup(Executable(largeAddressAware: true, steamProtected: false));

        var result = _host.Run("mods", "activate", _controller, "-p", "Pad", "--dry-run");

        Assert.Equal(ExitCodes.Success, result.ExitCode);
        Assert.DoesNotContain("4 GB patch", result.Output, StringComparison.Ordinal);
    }

    /// <summary>A made-up 32-bit PE image; with <paramref name="steamProtected"/> its sections are <c>.text</c> and Steam's <c>.bind</c>.</summary>
    private static byte[] Executable(bool largeAddressAware, bool steamProtected)
    {
        var bytes = new byte[512];
        "MZ"u8.CopyTo(bytes);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(0x3C), 0x80);
        "PE\0\0"u8.CopyTo(bytes.AsSpan(0x80));
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(0x84), 0x014C);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(0x84 + 16), 224);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(0x84 + 18), largeAddressAware ? (ushort)0x0122 : (ushort)0x0102);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(0x98), 0x010B);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(0x98 + 92), 16);
        if (steamProtected)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(0x84 + 2), 2);
            ".text"u8.CopyTo(bytes.AsSpan(0x98 + 224));
            ".bind"u8.CopyTo(bytes.AsSpan(0x98 + 224 + 40));
        }

        return bytes;
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

    private void Setup(byte[] executable)
    {
        _host.FileSystem.AddFile(GamePaths.GameExecutable(_install), new MockFileData(executable));
        _host.Run("init", "Pad");
        _host.Run("profile", "set-path", "Pad", "install", _install);
        _host.Run("profile", "set-path", "Pad", "documents", _documents);

        var path = Path.Combine(MockUnixSupport.Path(@"C:\Downloads"), "controller.zip");
        using var buffer = new MemoryStream();
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (entry, bytes) in new[] { ("bin_ship/dinput8.dll", Dll()), ("bin_ship/DAControllerFramework.ini", "[pad]"u8.ToArray()) })
            {
                using var stream = zip.CreateEntry(entry).Open();
                stream.Write(bytes);
            }
        }

        _host.FileSystem.AddFile(path, new MockFileData(buffer.ToArray()));
        var output = _host.Run("mods", "import", path).Output;
        _controller = output[(output.IndexOf(" as ", StringComparison.Ordinal) + 4)..output.IndexOf(',', StringComparison.Ordinal)];
    }
}
