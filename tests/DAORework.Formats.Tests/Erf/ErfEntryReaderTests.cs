using DAORework.Formats.Erf;
using Xunit;

namespace DAORework.Formats.Tests.Erf;

/// <summary>Reading one file's data out of an ERF, and refusing what can't be read as plain data (P2.10-03).</summary>
public sealed class ErfEntryReaderTests
{
    private static readonly (string Name, byte[] Data)[] _files = [("hair.dds", [1, 2, 3]), ("chargenmorphcfg.xml", [60, 109, 47, 62])];

    [Fact]
    public void Read_V20_GivesTheEntrysBytes()
    {
        using var stream = new MemoryStream(TestErf.Build("V2.0", 0, _files));
        var toc = ErfReader.ReadTableOfContents(stream);

        Assert.Equal(new byte[] { 60, 109, 47, 62 }, ErfEntryReader.Read(stream, toc, toc.Entries[1]));
        Assert.Equal(new byte[] { 1, 2, 3 }, ErfEntryReader.Read(stream, toc, toc.Entries[0]));
    }

    [Theory]
    [InlineData(0x20u)] // Encrypted.
    [InlineData(0x20000000u)] // Compressed.
    public void Read_AnEncryptedOrCompressedArchive_IsRefused(uint flags)
    {
        using var stream = new MemoryStream(TestErf.Build("V2.2", flags, _files));
        var toc = ErfReader.ReadTableOfContents(stream);

        Assert.Throws<InvalidDataException>(() => ErfEntryReader.Read(stream, toc, toc.Entries[0]));
    }

    [Fact]
    public void Read_AnEntryWhoseSizesDiffer_IsRefused()
    {
        using var stream = new MemoryStream(TestErf.Build("V2.2", 0, _files)); // The test archive says unpacked is twice packed.
        var toc = ErfReader.ReadTableOfContents(stream);

        Assert.Throws<InvalidDataException>(() => ErfEntryReader.Read(stream, toc, toc.Entries[0]));
    }

    [Fact]
    public void Read_AnEntryPastTheEnd_OrTooLarge_IsRefusedBeforeAnythingIsAllocated()
    {
        using var stream = new MemoryStream(TestErf.Build("V2.0", 0, _files));
        var toc = ErfReader.ReadTableOfContents(stream);

        Assert.Throws<InvalidDataException>(() => ErfEntryReader.Read(stream, toc, toc.Entries[0] with { Offset = 1000 }));
        var huge = toc.Entries[0] with { PackedSize = uint.MaxValue, UnpackedSize = uint.MaxValue };
        Assert.Throws<InvalidDataException>(() => ErfEntryReader.Read(stream, toc, huge));
    }
}
