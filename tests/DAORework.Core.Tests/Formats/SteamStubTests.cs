using System.Buffers.Binary;
using DAORework.Core.Formats;
using Xunit;

namespace DAORework.Core.Tests.Formats;

/// <summary>ADR-0023: Steam's protection is recognised by its <c>.bind</c> section, read from the section table only.</summary>
public sealed class SteamStubTests
{
    private const int CoffOffset = 0x84;
    private const int TableOffset = CoffOffset + 20 + 224;

    [Fact]
    public void ImageWithABindSection_IsProtected()
    {
        var image = Image(".text", ".rdata", ".bind");

        Assert.Equal([".text", ".rdata", ".bind"], SteamStub.SectionNames(image));
        Assert.True(SteamStub.IsProtected(image));
    }

    [Fact]
    public void ImageWithoutOne_IsNot()
    {
        // The section list of the community's decrypted daorigins.exe, as read from its header.
        Assert.False(SteamStub.IsProtected(Image(".text", ".rdata", ".data", ".tls", ".rsrc")));
    }

    [Fact]
    public void SectionTableCutOffByTheBytesRead_ReadsWhatIsThere()
    {
        var image = Image(".text", ".bind");

        Assert.Equal([".text"], SteamStub.SectionNames(image.AsSpan(0, TableOffset + 40)));
        Assert.False(SteamStub.IsProtected(image.AsSpan(0, TableOffset + 40)));
    }

    [Fact]
    public void NotAnExecutable_HasNoSections()
    {
        Assert.Empty(SteamStub.SectionNames("not an executable at all, just text padding it out to sixty-four+ bytes"u8));
        Assert.False(SteamStub.IsProtected([]));
    }

    private static byte[] Image(params string[] sections)
    {
        var bytes = new byte[TableOffset + (sections.Length * 40)];
        bytes[0] = (byte)'M';
        bytes[1] = (byte)'Z';
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(0x3C), 0x80);
        "PE\0\0"u8.CopyTo(bytes.AsSpan(0x80));
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(CoffOffset), 0x014C);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(CoffOffset + 2), (ushort)sections.Length);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(CoffOffset + 16), 224);
        for (var i = 0; i < sections.Length; i++)
        {
            System.Text.Encoding.ASCII.GetBytes(sections[i]).CopyTo(bytes.AsSpan(TableOffset + (i * 40)));
        }

        return bytes;
    }
}
