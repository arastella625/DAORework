using System.IO.Abstractions.TestingHelpers;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Nodes;
using DAORework.Application.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Library;
using DAORework.Core.Mods.Safety;
using DAORework.Infrastructure.Mods;
using DAORework.Infrastructure.Persistence;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace DAORework.Infrastructure.Tests.Mods;

/// <summary><c>mod.json</c> schema 2 (ADR-0015): every entry says where its mod came from, and version 1 entries are read as downloads.</summary>
public sealed class LibraryEntrySchemaTests
{
    private static readonly string _appData = MockUnixSupport.Path(@"C:\AppData\DAORework");
    private static readonly string _download = MockUnixSupport.Path(@"C:\Users\Me\Downloads\Alpha.zip");

    private readonly MockFileSystem _fileSystem = new();
    private readonly FolderModLibrary _library;

    public LibraryEntrySchemaTests()
    {
        _library = new FolderModLibrary(_fileSystem, Options.Create(new DaoReworkOptions { AppDataDirectory = _appData }), new FakeTimeProvider());
        _fileSystem.AddFile(_download, new MockFileData(Zip("a.dds")));
    }

    [Fact]
    public async Task Import_NewEntry_WritesSchemaTwoWithADownloadOrigin()
    {
        var entry = (await _library.ImportAsync(_download)).Value.Entry;

        var json = JsonNode.Parse(_fileSystem.File.ReadAllText(MetadataPath(entry)))!;
        Assert.Equal(LibraryEntry.CurrentSchemaVersion, json["schemaVersion"]!.GetValue<int>());
        Assert.Equal(nameof(ModOriginKind.Download), json["origin"]!["kind"]!.GetValue<string>());
    }

    [Fact]
    public async Task List_VersionOneEntry_IsMigratedToADownload()
    {
        var entry = (await _library.ImportAsync(_download)).Value.Entry;
        RewriteMetadata(entry, json =>
        {
            json.Remove("origin");
            json["schemaVersion"] = 1;
        });

        var listed = Assert.Single(await _library.ListAsync());

        Assert.Equal(LibraryEntry.CurrentSchemaVersion, listed.SchemaVersion);
        Assert.Equal(ModOrigin.Download, listed.Origin);
        Assert.Equal(entry.Mod.Id, listed.Mod.Id);
        Assert.Equal(entry.SourceSha256, listed.SourceSha256);
    }

    [Fact]
    public async Task List_AdoptedEntry_KeepsItsOriginAndPath()
    {
        var entry = (await _library.ImportAsync(_download)).Value.Entry;
        var adopted = entry with { Origin = new ModOrigin(ModOriginKind.Adopted, "packages/core/override/Alpha") };
        _fileSystem.File.WriteAllText(MetadataPath(entry), JsonSerializer.Serialize(adopted, JsonDefaults.Options));

        var listed = Assert.Single(await _library.ListAsync());

        Assert.Equal(adopted.Origin, listed.Origin);
    }

    [Fact]
    public async Task Import_EntryFromANewerBuild_IsRefusedAndKept()
    {
        var entry = (await _library.ImportAsync(_download)).Value.Entry;
        RewriteMetadata(entry, json => json["schemaVersion"] = 99);
        var before = _fileSystem.File.ReadAllText(MetadataPath(entry));

        var again = await _library.ImportAsync(_download);

        Assert.Equal(DiagnosticCodes.ModImportFailed, again.Error!.Code);
        Assert.Contains("Update DAORework", again.Error.Message, StringComparison.Ordinal);
        Assert.Equal(before, _fileSystem.File.ReadAllText(MetadataPath(entry)));
    }

    [Fact]
    public async Task Import_RenamedProgram_IsClassedAProgramAndTheEntryIsNotScanned()
    {
        var path = MockUnixSupport.Path(@"C:\Users\Me\Downloads\Sneaky.zip");
        _fileSystem.AddFile(path, new MockFileData(ZipOf(("hair.dds", "MZ\u0090 pretending to be a texture"), ("eyes.dds", "DDS eyes"))));

        var entry = (await _library.ImportAsync(path)).Value.Entry;

        Assert.Equal([("eyes.dds", ModFileKind.Data), ("hair.dds", ModFileKind.Program)], entry.Mod.Files.OrderBy(f => f.RelativePath, StringComparer.Ordinal).Select(f => (f.RelativePath, f.Kind)));
        Assert.Equal(MalwareScanStatus.NotScanned, entry.Scan.Status);
        var json = JsonNode.Parse(_fileSystem.File.ReadAllText(MetadataPath(entry)))!;
        Assert.Equal(LibraryEntry.CurrentSchemaVersion, json["schemaVersion"]!.GetValue<int>());
        Assert.Equal(nameof(MalwareScanStatus.NotScanned), json["scan"]!["status"]!.GetValue<string>());
    }

    [Fact]
    public async Task List_VersionTwoEntry_IsClassifiedOnceFromItsFilesAndSaved()
    {
        var path = MockUnixSupport.Path(@"C:\Users\Me\Downloads\Sneaky.zip");
        _fileSystem.AddFile(path, new MockFileData(ZipOf(("hair.dds", "MZ renamed"), ("eyes.dds", "DDS eyes"))));
        var entry = (await _library.ImportAsync(path)).Value.Entry;
        RewriteMetadata(entry, json =>
        {
            json["schemaVersion"] = 2;
            json.Remove("scan");
            foreach (var file in json["mod"]!["files"]!.AsArray())
            {
                file!.AsObject().Remove("kind");
            }
        });

        var listed = (await _library.ListAsync()).Single(e => e.Mod.Id == entry.Mod.Id);

        Assert.Equal(ModFileKind.Program, listed.Mod.Files.Single(f => f.RelativePath == "hair.dds").Kind);
        Assert.Equal(ModFileKind.Data, listed.Mod.Files.Single(f => f.RelativePath == "eyes.dds").Kind);
        Assert.Contains("\"kind\": \"Program\"", _fileSystem.File.ReadAllText(MetadataPath(entry)), StringComparison.Ordinal);
    }

    private static byte[] ZipOf(params (string Name, string Content)[] entries)
    {
        using var buffer = new MemoryStream();
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (name, content) in entries)
            {
                using var writer = new StreamWriter(zip.CreateEntry(name).Open());
                writer.Write(content);
            }
        }

        return buffer.ToArray();
    }

    private static byte[] Zip(string name)
    {
        using var buffer = new MemoryStream();
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            using var stream = zip.CreateEntry(name).Open();
            stream.Write("a"u8);
        }

        return buffer.ToArray();
    }

    private string MetadataPath(LibraryEntry entry) => Path.Combine(_appData, "library", entry.Mod.Id + ".mod.json");

    private void RewriteMetadata(LibraryEntry entry, Action<JsonObject> change)
    {
        var json = JsonNode.Parse(_fileSystem.File.ReadAllText(MetadataPath(entry)))!.AsObject();
        change(json);
        _fileSystem.File.WriteAllText(MetadataPath(entry), json.ToJsonString());
    }
}
