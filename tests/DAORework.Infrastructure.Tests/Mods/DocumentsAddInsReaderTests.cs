using System.IO.Abstractions.TestingHelpers;
using System.Text;
using DAORework.Core.Diagnostics;
using DAORework.Infrastructure.Mods;
using Xunit;

namespace DAORework.Infrastructure.Tests.Mods;

/// <summary>The add-ins the game loads and the names each gives it (P2.5-23), from made-up folders and archives.</summary>
public sealed class DocumentsAddInsReaderTests
{
    private static readonly string _documents = MockUnixSupport.Path(@"C:\Users\Me\Documents\BioWare\Dragon Age");

    private readonly MockFileSystem _fileSystem = new();

    [Fact]
    public async Task Read_ListsLooseFilesAndArchiveNames_CoreApartFromModule()
    {
        List("""<AddInItem UID="Extra_Dog_Slot" ExtendedModuleUID="Single Player" Priority="100" Enabled="1" />""");
        Add("AddIns/extra_dog_slot/core/data/Extra_Dog_Slot_modcore.erf", Erf("party_base.ncs", "dog.uti"));
        Add("AddIns/extra_dog_slot/core/override/dog.dds", "d"u8.ToArray());
        Add("AddIns/extra_dog_slot/module/data/Extra_Dog_Slot_module.erf", Erf("manifest.xml", "den_area.are"));
        Add("AddIns/extra_dog_slot/module/Extra_Dog_Slot.cif", "c"u8.ToArray());
        Add("AddIns/extra_dog_slot/core/data_tools/arttools.erf", Erf("tool.mmh"));

        var addIns = await ReadAsync();

        var dog = Assert.Single(addIns.Contents);
        Assert.Equal("Extra_Dog_Slot", dog.AddIn.Uid);
        Assert.Equal(["dog.dds", "dog.uti", "party_base.ncs"], dog.CoreNames.Order(StringComparer.Ordinal));
        Assert.Equal(["Extra_Dog_Slot.cif", "den_area.are", "manifest.xml"], dog.ModuleNames.Order(StringComparer.Ordinal));
        Assert.Empty(addIns.Unreadable);
    }

    [Fact]
    public async Task Read_AnAddInWithoutAFolderHere_IsListedButNotCompared()
    {
        List("""<AddInItem UID="DAO_PRC_CP_2" Priority="100" Enabled="1" BioWare="1" /><AddInItem UID="../escape" Priority="1" Enabled="1" />""");
        Add("AddIns/other/core/override/x.txt", "x"u8.ToArray());

        var addIns = await ReadAsync();

        Assert.Equal(["DAO_PRC_CP_2", "../escape"], addIns.Listed.Select(a => a.Uid));
        Assert.Empty(addIns.Contents);
    }

    [Fact]
    public async Task Read_ADamagedArchive_IsNotedAndTheRestStillRead()
    {
        List("""<AddInItem UID="mod" Priority="100" Enabled="1" />""");
        Add("AddIns/mod/core/data/broken.erf", "not an archive"u8.ToArray());
        Add("AddIns/mod/core/override/fine.gda", "g"u8.ToArray());

        var addIns = await ReadAsync();

        Assert.Equal(["fine.gda"], Assert.Single(addIns.Contents).CoreNames);
        Assert.Equal(["AddIns/mod/core/data/broken.erf"], addIns.Unreadable);
    }

    [Fact]
    public async Task Read_NoAddInsList_IsNoAddIns_AndABrokenOneIsAnError()
    {
        Assert.Empty((await ReadAsync()).Listed);

        Add("Settings/AddIns.xml", "<AddInsList>"u8.ToArray());
        var broken = await new DocumentsAddInsReader(_fileSystem).ReadAsync(_documents);

        Assert.Equal(DiagnosticCodes.AddInsListUnreadable, broken.Error!.Code);
    }

    /// <summary>An ERF V2.0 table of contents from the format's public specification, with one byte of data per name.</summary>
    private static byte[] Erf(params string[] names)
    {
        using var buffer = new MemoryStream();
        using var writer = new BinaryWriter(buffer);
        writer.Write(Encoding.Unicode.GetBytes("ERF V2.0"));
        writer.Write((uint)names.Length);
        writer.Write(109u);
        writer.Write(300u);
        writer.Write(uint.MaxValue);
        var offset = (uint)(32 + (72 * names.Length));
        foreach (var name in names)
        {
            var bytes = new byte[64];
            Encoding.Unicode.GetBytes(name).CopyTo(bytes, 0);
            writer.Write(bytes);
            writer.Write(offset++);
            writer.Write(1u);
        }

        writer.Write(new byte[names.Length]);
        return buffer.ToArray();
    }

    private void List(string items) => Add("Settings/AddIns.xml", Encoding.UTF8.GetBytes($"<AddInsList>{items}</AddInsList>"));

    private void Add(string relative, byte[] data) =>
        _fileSystem.AddFile(Path.GetFullPath(Path.Combine(_documents, relative.Replace('/', Path.DirectorySeparatorChar))), new MockFileData(data));

    private async Task<Core.Mods.AddIns.DocumentsAddIns> ReadAsync() => (await new DocumentsAddInsReader(_fileSystem).ReadAsync(_documents)).Value;
}
