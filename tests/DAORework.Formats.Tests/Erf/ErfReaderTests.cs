using DAORework.Formats.Erf;
using Xunit;

namespace DAORework.Formats.Tests.Erf;

/// <summary>Reading an ERF's table of contents, and refusing what doesn't add up (<c>docs/formats/erf.md</c>).</summary>
public sealed class ErfReaderTests
{
    private static readonly (string Name, byte[] Data)[] _files = [("hair.dds", [1, 2, 3]), ("abi_base.gda", [4])];

    [Fact]
    public void Read_V20_GivesEachFilesNameAndPlace()
    {
        var toc = Read(TestErf.Build("V2.0", 0, _files));

        Assert.Equal("V2.0", toc.Version);
        Assert.Equal(["hair.dds", "abi_base.gda"], toc.Entries.Select(e => e.Name));
        Assert.Equal(32u + (72 * 2), toc.Entries[0].Offset);
        Assert.Equal(3u, toc.Entries[0].PackedSize);
        Assert.Equal(3u, toc.Entries[0].UnpackedSize);
    }

    [Fact]
    public void Read_V22_GivesBothSizesAndTheFlags()
    {
        var toc = Read(TestErf.Build("V2.2", 0x20, _files));

        Assert.Equal("V2.2", toc.Version);
        Assert.True(toc.IsEncrypted);
        Assert.Equal(3u, toc.Entries[0].PackedSize);
        Assert.Equal(6u, toc.Entries[0].UnpackedSize);
    }

    [Fact]
    public void Read_AnEmptyArchive_HasNoEntries()
    {
        Assert.Empty(Read(TestErf.Build("V2.0")).Entries);
    }

    [Theory]
    [InlineData("")]
    [InlineData("PK")]
    [InlineData("this is a text file, long enough to hold a header but nothing like one at all")]
    public void Read_NotAnErf_IsRefused(string text)
    {
        Assert.Throws<InvalidDataException>(() => Read(System.Text.Encoding.ASCII.GetBytes(text)));
    }

    [Fact]
    public void Read_DragonAge2sVersion_IsRefused()
    {
        var bytes = TestErf.Build("V2.0");
        System.Text.Encoding.Unicode.GetBytes("ERF V3.0").CopyTo(bytes, 0);

        Assert.Throws<InvalidDataException>(() => Read(bytes));
    }

    [Fact]
    public void Read_ACountTheFileCouldNotHold_IsRefusedBeforeAnythingIsAllocated()
    {
        var bytes = TestErf.Build("V2.0", 0, _files);
        BitConverter.GetBytes(uint.MaxValue).CopyTo(bytes, 16);

        Assert.Throws<InvalidDataException>(() => Read(bytes));
    }

    [Fact]
    public void Read_AnEntryPointingPastTheEnd_IsRefused()
    {
        var bytes = TestErf.Build("V2.0", 0, _files);
        BitConverter.GetBytes(uint.MaxValue).CopyTo(bytes, 32 + 64);

        Assert.Throws<InvalidDataException>(() => Read(bytes));
    }

    [Fact]
    public void Read_ANameFillingItsField_IsKeptWhole()
    {
        var name = new string('a', 32);

        Assert.Equal(name, Assert.Single(Read(TestErf.Build("V2.0", 0, (name, [1]))).Entries).Name);
    }

    /// <summary>Any cut, and any corrupted byte, ends in a table or in <see cref="InvalidDataException"/>, never in anything else.</summary>
    [Fact]
    public void Read_DamagedFiles_NeverThrowAnythingElse()
    {
        var random = new Random(47810);
        foreach (var version in new[] { "V2.0", "V2.2" })
        {
            var good = TestErf.Build(version, 0, _files);
            for (var cut = 0; cut < good.Length; cut++)
            {
                Survives(good[..cut]);
            }

            for (var round = 0; round < 2000; round++)
            {
                var bytes = (byte[])good.Clone();
                bytes[random.Next(bytes.Length)] = (byte)random.Next(256);
                bytes[random.Next(bytes.Length)] = (byte)random.Next(256);
                Survives(bytes);
            }
        }
    }

    /// <summary>A check for the owner's own install: nothing of the game is ever committed. It runs only with DAOREWORK_GAME_DIR set.</summary>
    [Fact]
    public void Read_EveryErfInARealInstall_Works()
    {
        var game = Environment.GetEnvironmentVariable("DAOREWORK_GAME_DIR");
        if (string.IsNullOrEmpty(game))
        {
            return;
        }

        var archives = Directory.EnumerateFiles(game, "*.erf", SearchOption.AllDirectories).ToList();

        Assert.NotEmpty(archives);
        Assert.All(archives, path =>
        {
            using var file = File.OpenRead(path);
            Assert.NotNull(ErfReader.ReadTableOfContents(file));
        });
    }

    private static void Survives(byte[] bytes)
    {
        try
        {
            Read(bytes);
        }
        catch (InvalidDataException)
        {
            // Refused, which is one of the two allowed ends.
        }
    }

    private static ErfTableOfContents Read(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        return ErfReader.ReadTableOfContents(stream);
    }
}
