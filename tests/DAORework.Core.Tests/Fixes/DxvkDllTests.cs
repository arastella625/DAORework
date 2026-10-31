using System.Buffers.Binary;
using System.Text;
using DAORework.Core.Diagnostics;
using DAORework.Core.Fixes;
using Xunit;

namespace DAORework.Core.Tests.Fixes;

public sealed class DxvkDllTests
{
    private const string Marker = "DXVK: ";

    [Theory]
    [InlineData("v2.3", "2.3")]
    [InlineData("1.10.3", "1.10.3")]
    public void Identify_A32BitDxvkDll_ReadsItsVersion(string text, string expected)
    {
        Assert.Equal(expected, DxvkDll.Identify(Dll(text), Marker).Value);
    }

    [Fact]
    public void Identify_NoTextAfterTheMarker_IsOfUnknownVersion()
    {
        Assert.Equal(DxvkDll.UnknownVersion, DxvkDll.Identify(Dll(string.Empty), Marker).Value);
    }

    [Fact]
    public void Identify_The64BitBuild_IsRefusedWithWhichToTake()
    {
        var result = DxvkDll.Identify(Dll("v2.3", machine: 0x8664), Marker);

        Assert.Equal(DiagnosticCodes.FixSourceInvalid, result.Error!.Code);
        Assert.Contains("x32 folder", result.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Identify_AnotherD3d9Dll_IsNotDxvk()
    {
        Assert.Contains("not a DXVK", DxvkDll.Identify(Dll(null), Marker).Error!.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Identify_MarkerWithoutAPeHeader_IsNotAWindowsDll()
    {
        Assert.Contains("not a Windows DLL", DxvkDll.Identify(Encoding.ASCII.GetBytes("\0DXVK: \0v2.3\0"), Marker).Error!.Message, StringComparison.Ordinal);
    }

    /// <summary>A made-up PE image with DXVK's marker, "DXVK: " and a NUL, then the version text, as DXVK's d3d9.dll carries it.</summary>
    internal static byte[] Dll(string? version, ushort machine = 0x014C)
    {
        var header = new byte[256];
        "MZ"u8.CopyTo(header);
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(0x3C), 0x80);
        "PE\0\0"u8.CopyTo(header.AsSpan(0x80));
        BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(0x84), machine);
        var marker = version is null ? "\0ReShade\0" : $"\0{Marker}\0{version}\0";
        return [.. header, .. Encoding.ASCII.GetBytes(marker)];
    }
}
