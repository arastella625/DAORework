using System.Buffers.Binary;
using System.Text;
using DAORework.Core.Catalog;

namespace DAORework.Application.Tests.Fixes;

/// <summary>Made-up DXVK DLLs and the catalog entry that recognizes them.</summary>
internal static class TestDxvk
{
    public const string Marker = "DXVK: ";

    public static KnownItem CatalogItem { get; } = new(
        "dxvk", "DXVK", KnownItemKind.Fix, "https://github.com/doitsujin/dxvk", "zlib/libpng", [new KnownItemFingerprint(FingerprintKind.GameFile, "bin_ship/d3d9.dll", Marker)]);

    public static byte[] Dll(string version, ushort machine = 0x014C) => Build(machine, $"\0{Marker}\0v{version}\0");

    public static byte[] NotDxvk() => Build(0x014C, "\0ReShade 5.9\0");

    private static byte[] Build(ushort machine, string text)
    {
        var header = new byte[256];
        "MZ"u8.CopyTo(header);
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(0x3C), 0x80);
        "PE\0\0"u8.CopyTo(header.AsSpan(0x80));
        BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(0x84), machine);
        return [.. header, .. Encoding.ASCII.GetBytes(text)];
    }
}
