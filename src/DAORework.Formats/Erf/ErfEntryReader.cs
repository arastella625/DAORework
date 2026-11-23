namespace DAORework.Formats.Erf;

/// <summary>
/// Reads one file's data out of an ERF archive (<c>docs/formats/erf.md</c>), after <see cref="ErfReader"/> has read the
/// table. Only plain data is read: an encrypted or compressed archive, an entry whose sizes differ, one that points past
/// the end of the file or one larger than <see cref="MaxEntryBytes"/> is refused before anything is allocated.
/// </summary>
public static class ErfEntryReader
{
    /// <summary>The largest entry read, far above any text file the game keeps in an archive.</summary>
    public const int MaxEntryBytes = 16 * 1024 * 1024;

    /// <exception cref="InvalidDataException">The entry can't be read as plain data.</exception>
    public static byte[] Read(Stream stream, ErfTableOfContents tableOfContents, ErfEntry entry)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(tableOfContents);
        ArgumentNullException.ThrowIfNull(entry);
        if (tableOfContents.IsEncrypted || tableOfContents.IsCompressed || entry.PackedSize != entry.UnpackedSize)
        {
            throw new InvalidDataException($"{entry.Name} is encrypted or compressed.");
        }

        if (entry.PackedSize > MaxEntryBytes)
        {
            throw new InvalidDataException($"{entry.Name} is larger than {MaxEntryBytes} bytes.");
        }

        if ((long)entry.Offset + entry.PackedSize > stream.Length)
        {
            throw new InvalidDataException($"{entry.Name} points past the end of the file.");
        }

        var data = new byte[entry.PackedSize];
        stream.Seek(entry.Offset, SeekOrigin.Begin);
        stream.ReadExactly(data);
        return data;
    }
}
