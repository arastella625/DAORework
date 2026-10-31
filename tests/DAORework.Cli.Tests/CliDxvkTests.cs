using System.Buffers.Binary;
using System.IO.Abstractions.TestingHelpers;
using System.IO.Compression;
using System.Text;
using DAORework.Cli.Composition;
using Xunit;

namespace DAORework.Cli.Tests;

/// <summary>fixes apply dxvk and remove dxvk end to end, with a made-up DXVK release recognized by the embedded catalog.</summary>
public sealed class CliDxvkTests : IDisposable
{
    private static readonly string _install = MockUnixSupport.Path(@"D:\Games\Dragon Age");
    private static readonly string _dll = Path.Combine(_install, "bin_ship", "d3d9.dll");
    private static readonly string _release = MockUnixSupport.Path(@"C:\Downloads\dxvk-2.3.zip");
    private static readonly byte[] _x32 = Dll(0x014C);

    private readonly CliTestHost _host = new();

    public CliDxvkTests()
    {
        _host.FileSystem.AddFile(Path.Combine(_install, "bin_ship", "daorigins.exe"), new MockFileData("exe"));
        _host.FileSystem.AddFile(_release, new MockFileData(Release()));
        _host.Run("init", "Dxvk");
        _host.Run("profile", "set-path", "Dxvk", "install", _install);
    }

    public void Dispose() => _host.Dispose();

    [Fact]
    public void ApplyThenRemove_PlacesTheX32DllAndThenDeletesIt()
    {
        var applied = _host.Run("fixes", "apply", "dxvk", _release, "-p", "Dxvk", "--yes");

        Assert.Equal(ExitCodes.Success, applied.ExitCode);
        Assert.Contains("  Add bin_ship/d3d9.dll", applied.Output, StringComparison.Ordinal);
        Assert.Equal(_x32, _host.FileSystem.File.ReadAllBytes(_dll));
        Assert.Contains("DXVK 2.3 (dxvk-2-3-", _host.Run("fixes", "status", "-p", "Dxvk").Output, StringComparison.Ordinal);

        var removed = _host.Run("fixes", "remove", "dxvk", "-p", "Dxvk", "--yes");

        Assert.Equal(ExitCodes.Success, removed.ExitCode);
        Assert.False(_host.FileSystem.File.Exists(_dll));
    }

    [Fact]
    public void Apply_OverAnotherWrapper_IsBlockedAndLeavesIt()
    {
        _host.FileSystem.AddFile(_dll, new MockFileData("reshade"));

        var result = _host.Run("fixes", "apply", "dxvk", _release, "-p", "Dxvk", "--yes");

        Assert.Equal(ExitCodes.Failure, result.ExitCode);
        Assert.Contains("ReShade or another Direct3D wrapper", result.Output, StringComparison.Ordinal);
        Assert.Equal("reshade", _host.FileSystem.File.ReadAllText(_dll));
    }

    [Fact]
    public void Apply_WithoutARelease_SaysWhereToGetOne()
    {
        var result = _host.Run("fixes", "apply", "dxvk", "-p", "Dxvk", "--yes");

        Assert.Equal(ExitCodes.Failure, result.ExitCode);
        Assert.Contains("github.com/doitsujin/dxvk/releases", result.Output, StringComparison.Ordinal);
    }

    /// <summary>A made-up PE image carrying DXVK's version marker, as the release's d3d9.dll does.</summary>
    private static byte[] Dll(ushort machine)
    {
        var header = new byte[256];
        "MZ"u8.CopyTo(header);
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(0x3C), 0x80);
        "PE\0\0"u8.CopyTo(header.AsSpan(0x80));
        BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(0x84), machine);
        return [.. header, .. Encoding.ASCII.GetBytes("\0DXVK: \0v2.3\0")];
    }

    private static byte[] Release()
    {
        using var buffer = new MemoryStream();
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (name, content) in new[] { ("dxvk-2.3/x64/d3d9.dll", Dll(0x8664)), ("dxvk-2.3/x32/d3d9.dll", _x32) })
            {
                using var stream = zip.CreateEntry(name).Open();
                stream.Write(content);
            }
        }

        return buffer.ToArray();
    }
}
