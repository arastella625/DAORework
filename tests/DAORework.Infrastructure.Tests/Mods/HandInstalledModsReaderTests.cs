using System.IO.Abstractions.TestingHelpers;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Adoption;
using DAORework.Infrastructure.Mods;
using Xunit;

namespace DAORework.Infrastructure.Tests.Mods;

public sealed class HandInstalledModsReaderTests
{
    private static readonly string _documents = MockUnixSupport.Path(@"C:\Users\Me\Documents\BioWare\Dragon Age");

    private readonly MockFileSystem _fileSystem = new();

    [Fact]
    public async Task Read_ListsAddInsWithTheirTitlesFoldersAndOverrideFiles()
    {
        Add("Settings/AddIns.xml", """
            <AddInsList>
              <AddInItem UID="Extra_Dog_Slot" Name="Extra Dog Slot"><Title DefaultText="Extra Dog Slot v1.41" /></AddInItem>
              <AddInItem UID="nameless" />
            </AddInsList>
            """);
        Add("AddIns/Extra_Dog_Slot/core/dog.gda", "dog");
        Add("packages/core/override/Better Hair/textures/hair.dds", "hair");
        Add("packages/core/override/loose.uti", "l");

        var snapshot = await ReadAsync();

        Assert.Equal([new RegisteredAddIn("Extra_Dog_Slot", "Extra Dog Slot v1.41"), new RegisteredAddIn("nameless", "nameless")], snapshot.RegisteredAddIns);
        Assert.Equal([new HandInstalledFile("AddIns/Extra_Dog_Slot/core/dog.gda", 3)], snapshot.AddInFolders["extra_dog_slot"]);
        Assert.Equal([new HandInstalledFile("packages/core/override/Better Hair/textures/hair.dds", 4)], snapshot.OverrideFolders["Better Hair"]);
        Assert.Equal([new HandInstalledFile("packages/core/override/loose.uti", 1)], snapshot.LooseOverrideFiles);
        Assert.False(snapshot.VortexDeployedOverride);
        Assert.False(snapshot.VortexDeployedAddIns);
    }

    [Fact]
    public async Task Read_VortexManifestInOverride_IsFlaggedAndNotALooseFile()
    {
        Add("packages/core/override/vortex.deployment.json", "{}");
        Add("packages/core/override/loose.uti", "l");

        var snapshot = await ReadAsync();

        Assert.True(snapshot.VortexDeployedOverride);
        Assert.False(snapshot.VortexDeployedAddIns);
        Assert.Equal(["packages/core/override/loose.uti"], snapshot.LooseOverrideFiles.Select(f => f.Path));
    }

    [Fact]
    public async Task Read_FolderTheGameHasNotWrittenYet_IsEmpty()
    {
        var snapshot = await ReadAsync();

        Assert.Empty(snapshot.RegisteredAddIns);
        Assert.Empty(snapshot.AddInFolders);
        Assert.Empty(snapshot.OverrideFolders);
        Assert.Empty(snapshot.LooseOverrideFiles);
    }

    [Fact]
    public async Task Read_UnreadableAddInsList_FailsWithTheAdoptScanCode()
    {
        Add("Settings/AddIns.xml", "<AddInsList>");

        var result = await new HandInstalledModsReader(_fileSystem).ReadAsync(_documents);

        Assert.Equal(DiagnosticCodes.ModAdoptScanFailed, result.Error!.Code);
    }

    private void Add(string relative, string content) =>
        _fileSystem.AddFile(Path.Combine(_documents, relative.Replace('/', Path.DirectorySeparatorChar)), new MockFileData(content));

    private async Task<HandInstalledSnapshot> ReadAsync() => (await new HandInstalledModsReader(_fileSystem).ReadAsync(_documents)).Value;
}
