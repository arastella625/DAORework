using System.IO.Abstractions.TestingHelpers;
using DAORework.Application.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Adoption;
using DAORework.Core.Mods.Library;
using DAORework.Infrastructure.Mods;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace DAORework.Infrastructure.Tests.Mods;

/// <summary>Importing a hand-installed mod into the library from where it is (ADR-0015).</summary>
public sealed class HandInstalledModImporterTests
{
    private const string Item = "<AddInItem UID=\"Extra_Dog_Slot\" Name=\"Extra Dog Slot\" Enabled=\"1\" State=\"2\"><Title DefaultText=\"Extra Dog Slot v1.41\" /></AddInItem>";

    private static readonly string _appData = MockUnixSupport.Path(@"C:\AppData\DAORework");
    private static readonly string _documents = MockUnixSupport.Path(@"C:\Users\Me\Documents\BioWare\Dragon Age");

    private readonly MockFileSystem _fileSystem = new();
    private readonly HandInstalledModImporter _importer;

    public HandInstalledModImporterTests()
    {
        _importer = new HandInstalledModImporter(_fileSystem, Options.Create(new DaoReworkOptions { AppDataDirectory = _appData }), new FakeTimeProvider());
        Add("Settings/AddIns.xml", $"<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<AddInsList>\r\n  {Item}\r\n</AddInsList>");
        Add("AddIns/Extra_Dog_Slot/core/dog.gda", "dog");
        Add("packages/core/override/Better Hair/textures/hair.dds", "hair");
    }

    [Fact]
    public async Task Import_AddIn_KeepsItsAddInItemAsItIsInAManifestAndIsAdopted()
    {
        var entry = (await _importer.ImportAsync(_documents, Candidate(AdoptionCandidateKind.AddIn, "AddIns/Extra_Dog_Slot", "AddIns/Extra_Dog_Slot/core/dog.gda"), "Extra Dog Slot v1.41")).Value;

        Assert.Equal(ModKind.AddIn, entry.Mod.Kind);
        Assert.Equal("Extra_Dog_Slot", entry.Mod.AddInUid);
        Assert.Equal(["Contents/addins/Extra_Dog_Slot/core/dog.gda", "Manifest.xml"], entry.Mod.Files.Select(f => f.RelativePath));
        Assert.Contains(Item, Library(entry, "Manifest.xml"), StringComparison.Ordinal);
        Assert.Equal("dog", Library(entry, "Contents/addins/Extra_Dog_Slot/core/dog.gda"));
        Assert.Equal(new ModOrigin(ModOriginKind.Adopted, "AddIns/Extra_Dog_Slot"), entry.Origin);
        Assert.Equal(["Contents/addins/Extra_Dog_Slot/core/dog.gda"], ModLayout.Map(entry.Mod).Value.Select(f => f.Source.RelativePath));
        Assert.Equal("AddIns/Extra_Dog_Slot/core/dog.gda", ModLayout.Map(entry.Mod).Value[0].TargetPath);
    }

    [Fact]
    public async Task Import_OverrideFolder_KeepsItsLayoutBelowTheFolderAndNeverChangesIt()
    {
        var entry = (await _importer.ImportAsync(_documents, Candidate(AdoptionCandidateKind.OverrideFolder, "packages/core/override/Better Hair", "packages/core/override/Better Hair/textures/hair.dds"), "Better Hair")).Value;
        var again = (await _importer.ImportAsync(_documents, Candidate(AdoptionCandidateKind.OverrideFolder, "packages/core/override/Better Hair", "packages/core/override/Better Hair/textures/hair.dds"), "Better Hair")).Value;

        Assert.Equal(ModKind.Override, entry.Mod.Kind);
        Assert.Equal("Better Hair", entry.Mod.Name);
        Assert.Equal(["textures/hair.dds"], entry.Mod.Files.Select(f => f.RelativePath));
        Assert.Equal(entry.Mod.Id, again.Mod.Id);
        Assert.Equal("hair", _fileSystem.File.ReadAllText(Path.Combine(_documents, "packages", "core", "override", "Better Hair", "textures", "hair.dds")));
    }

    [Fact]
    public async Task Import_AddInNoLongerRegistered_Fails()
    {
        Add("Settings/AddIns.xml", "<AddInsList />");

        var result = await _importer.ImportAsync(_documents, Candidate(AdoptionCandidateKind.AddIn, "AddIns/Extra_Dog_Slot", "AddIns/Extra_Dog_Slot/core/dog.gda"), "Extra Dog Slot");

        Assert.Equal(DiagnosticCodes.ModAdoptSourceMissing, result.Error!.Code);
    }

    private static AdoptionCandidate Candidate(AdoptionCandidateKind kind, string path, params string[] files) =>
        new($"id:{path}", kind, path, path, files.Select(f => new HandInstalledFile(f, 4)).ToList());

    private void Add(string relative, string content) =>
        _fileSystem.AddFile(Path.Combine(_documents, relative.Replace('/', Path.DirectorySeparatorChar)), new MockFileData(content));

    private string Library(LibraryEntry entry, string relative) =>
        _fileSystem.File.ReadAllText(Path.Combine(_appData, "library", entry.Mod.Id, relative.Replace('/', Path.DirectorySeparatorChar)));
}
