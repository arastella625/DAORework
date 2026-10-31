using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;

namespace DAORework.App.Tests;

/// <summary>Made-up files the fixes accept: nothing of the game's executable or of DXVK's own code.</summary>
internal static class FixFixtures
{
    /// <summary>A 32-bit PE32 image the 4 GB patch accepts: headers, then filler.</summary>
    public static byte[] Executable()
    {
        var bytes = Filler(7, 4096);
        bytes[0] = (byte)'M';
        bytes[1] = (byte)'Z';
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(0x3C), 0x80);
        "PE\0\0"u8.CopyTo(bytes.AsSpan(0x80));
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(0x84), 0x014C);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(0x84 + 16), 224);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(0x84 + 18), 0x0102);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(0x98), 0x010B);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(0x98 + 92), 16);
        return bytes;
    }

    /// <summary>Whether an executable's header has the large-address-aware flag.</summary>
    public static bool LargeAddressAware(byte[] executable) =>
        (BinaryPrimitives.ReadUInt16LittleEndian(executable.AsSpan(0x84 + 18)) & 0x0020) != 0;

    /// <summary>A release zip laid out as DXVK publishes it, with a made-up d3d9.dll for each architecture.</summary>
    public static byte[] DxvkRelease(string version)
    {
        using var buffer = new MemoryStream();
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (folder, machine) in new[] { ("x32", (ushort)0x014C), ("x64", (ushort)0x8664) })
            {
                using var stream = zip.CreateEntry($"dxvk-{version}/{folder}/d3d9.dll").Open();
                stream.Write(DxvkDll(version, machine));
            }
        }

        return buffer.ToArray();
    }

    /// <summary>A PE header for <paramref name="machine"/>, filler, then DXVK's version marker as the real DLL carries it.</summary>
    private static byte[] DxvkDll(string version, ushort machine)
    {
        var bytes = Filler(9, 4096);
        bytes[0] = (byte)'M';
        bytes[1] = (byte)'Z';
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(0x3C), 0x80);
        "PE\0\0"u8.CopyTo(bytes.AsSpan(0x80));
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(0x84), machine);
        return [.. bytes, .. Encoding.ASCII.GetBytes($"\0DXVK: \0v{version}\0")];
    }

    private static byte[] Filler(byte seed, int length) => Enumerable.Range(0, length).Select(i => (byte)((i * seed) % 251)).ToArray();
}
