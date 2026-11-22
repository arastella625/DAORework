using System.IO.Abstractions.TestingHelpers;
using System.Text;
using DAORework.Infrastructure.Mods;
using Xunit;

namespace DAORework.Infrastructure.Tests.Mods;

/// <summary>Finding the game's own chargenmorphcfg.xml in made-up installs (P2.10-03, ADR-0025 amended); no game file is used.</summary>
public sealed class GameChargenMorphSourceTests
{
    private static readonly string _install = MockUnixSupport.Path(@"D:\Games\Dragon Age");
    private static readonly string _data = Path.Combine(_install, "packages", "core", "data");

    private readonly MockFileSystem _fileSystem = new();

    [Fact]
    public async Task Read_FindsTheCopyInWhicheverArchiveNamesIt()
    {
        Add("2da.erf", Erf(0, ("abi_base.gda", "x"u8.ToArray())));
        Add("chargen.erf", Erf(0, ("hm_har.mmh", "m"u8.ToArray()), ("ChargenMorphCfg.xml", "<morph_config/>"u8.ToArray())));

        Assert.Equal("<morph_config/>", await ReadAsync());
    }

    [Fact]
    public async Task Read_ALooseCopy_ComesFirst()
    {
        Add("chargenmorphcfg.xml", "<morph_config><loose/></morph_config>"u8.ToArray());
        Add("chargen.erf", Erf(0, ("chargenmorphcfg.xml", "<morph_config/>"u8.ToArray())));

        Assert.Equal("<morph_config><loose/></morph_config>", await ReadAsync());
    }

    [Fact]
    public async Task Read_PassesOverWhatIsNotPlainData_OrNotAnArchive()
    {
        Add("broken.erf", "not an archive"u8.ToArray());
        Add("packed.erf", Erf(0x20000000, ("chargenmorphcfg.xml", "<morph_config/>"u8.ToArray())));

        Assert.Null(await ReadAsync());
    }

    [Fact]
    public async Task Read_AnInstallWithoutTheFolder_HasNoCopy()
    {
        Assert.Null(await ReadAsync());
    }

    private async Task<string?> ReadAsync()
    {
        var result = await new GameChargenMorphSource(_fileSystem).ReadAsync(_install, CancellationToken.None);
        Assert.True(result.IsSuccess);
        return result.Value;
    }

    private void Add(string name, byte[] data) => _fileSystem.AddFile(Path.Combine(_data, name), new MockFileData(data));

    /// <summary>An ERF V2.2 from the format's public specification, with the given flags and each entry's sizes equal.</summary>
    private static byte[] Erf(uint flags, params (string Name, byte[] Data)[] files)
    {
        using var buffer = new MemoryStream();
        using var writer = new BinaryWriter(buffer);
        writer.Write(Encoding.Unicode.GetBytes("ERF V2.2"));
        writer.Write((uint)files.Length);
        writer.Write(109u);
        writer.Write(300u);
        writer.Write(uint.MaxValue);
        writer.Write(flags);
        writer.Write(0u);
        writer.Write(new byte[16]);
        var offset = (uint)(56 + (76 * files.Length));
        foreach (var (name, data) in files)
        {
            var bytes = new byte[64];
            Encoding.Unicode.GetBytes(name).CopyTo(bytes, 0);
            writer.Write(bytes);
            writer.Write(offset);
            writer.Write((uint)data.Length);
            writer.Write((uint)data.Length);
            offset += (uint)data.Length;
        }

        foreach (var (_, data) in files)
        {
            writer.Write(data);
        }

        return buffer.ToArray();
    }
}
