using System.Text;

namespace DAORework.Formats.Erf;

/// <summary>
/// Reads the table of contents of an ERF V2.0 or V2.2 archive (<c>docs/formats/erf.md</c>). Only the header and the table
/// are read, never a file's data. Every number in the file is untrusted, so a table that couldn't fit in the file, or an
/// entry that points past its end, is refused before anything is allocated for it.
/// </summary>
public static class ErfReader
{
    private const int MagicBytes = 16;
    private const int NameBytes = 64;
    private const int HeaderV20 = MagicBytes + 16;
    private const int HeaderV22 = HeaderV20 + 24;
    private const int EntryV20 = NameBytes + 8;
    private const int EntryV22 = NameBytes + 12;

    /// <exception cref="InvalidDataException">The stream isn't an ERF archive this reader knows, or it is damaged.</exception>
    public static ErfTableOfContents ReadTableOfContents(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var reader = new BinaryReader(stream, Encoding.Unicode, leaveOpen: true);
        var version = ReadVersion(reader);
        var isV22 = version == "V2.2";
        var fileCount = ReadHeaderNumbers(reader);
        var flags = isV22 ? ReadExtraHeader(reader) : 0;
        var entrySize = isV22 ? EntryV22 : EntryV20;
        var header = isV22 ? HeaderV22 : HeaderV20;
        if ((long)fileCount * entrySize > stream.Length - header)
        {
            throw new InvalidDataException($"The table of {fileCount} entries doesn't fit in the file.");
        }

        var entries = new ErfEntry[fileCount];
        for (var i = 0; i < entries.Length; i++)
        {
            entries[i] = ReadEntry(reader, isV22, stream.Length);
        }

        return new ErfTableOfContents(version, flags, entries);
    }

    private static string ReadVersion(BinaryReader reader)
    {
        var bytes = reader.ReadBytes(MagicBytes);
        if (bytes.Length < MagicBytes)
        {
            throw new InvalidDataException("The file is too short to be an ERF archive.");
        }

        return Encoding.Unicode.GetString(bytes) switch
        {
            "ERF V2.0" => "V2.0",
            "ERF V2.2" => "V2.2",
            _ => throw new InvalidDataException("The file isn't an ERF V2.0 or V2.2 archive."),
        };
    }

    /// <summary>The file count, then the year, day and a field nobody has named, which say nothing about the contents.</summary>
    private static uint ReadHeaderNumbers(BinaryReader reader)
    {
        try
        {
            var fileCount = reader.ReadUInt32();
            reader.BaseStream.Seek(12, SeekOrigin.Current);
            return fileCount;
        }
        catch (EndOfStreamException exception)
        {
            throw new InvalidDataException("The header is cut short.", exception);
        }
    }

    /// <summary>The flags, then the module id and password digest, which only decrypting a file's data would need.</summary>
    private static uint ReadExtraHeader(BinaryReader reader)
    {
        try
        {
            var flags = reader.ReadUInt32();
            reader.BaseStream.Seek(20, SeekOrigin.Current);
            return flags;
        }
        catch (EndOfStreamException exception)
        {
            throw new InvalidDataException("The header is cut short.", exception);
        }
    }

    private static ErfEntry ReadEntry(BinaryReader reader, bool isV22, long fileLength)
    {
        var name = Encoding.Unicode.GetString(reader.ReadBytes(NameBytes));
        var end = name.IndexOf('\0', StringComparison.Ordinal);
        var offset = reader.ReadUInt32();
        var packed = reader.ReadUInt32();
        var unpacked = isV22 ? reader.ReadUInt32() : packed;
        if ((long)offset + packed > fileLength)
        {
            throw new InvalidDataException("An entry points past the end of the file.");
        }

        return new ErfEntry(end >= 0 ? name[..end] : name, offset, packed, unpacked);
    }
}
