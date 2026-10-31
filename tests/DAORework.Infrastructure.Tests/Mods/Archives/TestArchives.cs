using System.IO.Compression;
using System.Text;
using SharpCompress.Common;
using SharpCompress.Writers;

namespace DAORework.Infrastructure.Tests.Mods.Archives;

/// <summary>
/// Archives built in memory for the import tests, so none is committed: a <c>.7z</c> through SharpCompress's own
/// writer, and a <c>.rar</c> (version 5, files stored uncompressed) laid out as RARLAB's published format note
/// describes. Neither checks its names, so a path that leaves the mod's folder can be written on purpose.
/// </summary>
internal static class TestArchives
{
    public static byte[] SevenZip(params (string Name, string Content)[] files) => SevenZip(files.Select(f => (f.Name, Encoding.UTF8.GetBytes(f.Content))).ToArray());

    public static byte[] SevenZip(params (string Name, byte[] Content)[] files)
    {
        using var buffer = new MemoryStream();
        using (var writer = WriterFactory.OpenWriter(buffer, ArchiveType.SevenZip, new WriterOptions(CompressionType.LZMA) { LeaveStreamOpen = true }))
        {
            foreach (var (name, content) in files)
            {
                writer.Write(name, new MemoryStream(content), null);
            }
        }

        return buffer.ToArray();
    }

    public static byte[] Rar(params (string Name, string Content)[] files)
    {
        using var output = new MemoryStream();
        output.Write([0x52, 0x61, 0x72, 0x21, 0x1A, 0x07, 0x01, 0x00]);
        Block(output, [.. Number(1), .. Number(0), .. Number(0)], []);
        foreach (var (name, content) in files)
        {
            var data = Encoding.UTF8.GetBytes(content);
            var nameBytes = Encoding.UTF8.GetBytes(name);

            // A file header with a data area: file flags "CRC32 present", the size, attributes, the CRC32, method
            // "stored", host Windows, then the name.
            byte[] header =
            [
                .. Number(2), .. Number(2), .. Number(data.Length), .. Number(4), .. Number(data.Length), .. Number(0x20),
                .. BitConverter.GetBytes(Crc32(data)), .. Number(0), .. Number(0), .. Number(nameBytes.Length), .. nameBytes,
            ];
            Block(output, header, data);
        }

        Block(output, [.. Number(5), .. Number(0), .. Number(0)], []);
        return output.ToArray();
    }

    public static byte[] Zip(params (string Name, string Content)[] entries)
    {
        using var buffer = new MemoryStream();
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (name, content) in entries)
            {
                using var writer = new StreamWriter(zip.CreateEntry(name).Open());
                writer.Write(content);
            }
        }

        return buffer.ToArray();
    }

    private static void Block(Stream output, byte[] header, byte[] data)
    {
        byte[] sized = [.. Number(header.Length), .. header];
        output.Write(BitConverter.GetBytes(Crc32(sized)));
        output.Write(sized);
        output.Write(data);
    }

    private static byte[] Number(long value)
    {
        var bytes = new List<byte>();
        do
        {
            var low = (byte)(value & 0x7F);
            value >>= 7;
            bytes.Add(value > 0 ? (byte)(low | 0x80) : low);
        }
        while (value > 0);
        return [.. bytes];
    }

    private static uint Crc32(byte[] data)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var b in data)
        {
            crc ^= b;
            for (var bit = 0; bit < 8; bit++)
            {
                crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320u : crc >> 1;
            }
        }

        return ~crc;
    }
}
