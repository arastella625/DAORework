using System.Buffers.Binary;
using System.Text;

namespace DAORework.Core.Formats;

/// <summary>
/// Recognises Steam's copy protection (SteamStub) on an executable by its <c>.bind</c> section (ADR-0023). Detection only:
/// DAORework never removes or works around it (LEGAL_BOUNDARIES.md). Steam refuses to start a protected executable
/// whose bytes were changed, so the 4 GB patch is not applied to one.
/// </summary>
public static class SteamStub
{
    private const string BindSection = ".bind";
    private const int SectionCountOffset = 2;
    private const int OptionalHeaderSizeOffset = 16;
    private const int SectionHeaderLength = 40;
    private const int SectionNameLength = 8;

    /// <summary>Whether the executable whose leading bytes are given has a <c>.bind</c> section. False when its header cannot be read.</summary>
    public static bool IsProtected(ReadOnlySpan<byte> header) => SectionNames(header).Contains(BindSection, StringComparer.Ordinal);

    /// <summary>The names in the section table, as far as the given bytes reach.</summary>
    public static IReadOnlyList<string> SectionNames(ReadOnlySpan<byte> header)
    {
        if (!PortableExecutableHeader.TryFindCoffHeader(header, out var coff, out _))
        {
            return [];
        }

        var count = BinaryPrimitives.ReadUInt16LittleEndian(header[(coff + SectionCountOffset)..]);
        var optionalSize = BinaryPrimitives.ReadUInt16LittleEndian(header[(coff + OptionalHeaderSizeOffset)..]);
        var table = coff + PortableExecutableHeader.CoffHeaderLength + optionalSize;
        var names = new List<string>(count);
        for (var i = 0; i < count; i++)
        {
            var start = table + (i * SectionHeaderLength);
            if (start + SectionNameLength > header.Length)
            {
                break;
            }

            names.Add(Encoding.ASCII.GetString(header.Slice(start, SectionNameLength)).TrimEnd('\0'));
        }

        return names;
    }
}
