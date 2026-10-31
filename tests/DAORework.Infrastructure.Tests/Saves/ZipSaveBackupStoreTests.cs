using System.IO.Abstractions.TestingHelpers;
using System.IO.Compression;
using DAORework.Application.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Saves;
using DAORework.Infrastructure.Saves;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace DAORework.Infrastructure.Tests.Saves;

public sealed class ZipSaveBackupStoreTests
{
    private static readonly string _appData = MockUnixSupport.Path(@"C:\AppData\DAORework");
    private static readonly string _saves = MockUnixSupport.Path(@"C:\Users\Me\Documents\BioWare\Dragon Age\Characters");

    private readonly MockFileSystem _fileSystem = new();
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 9, 29, 20, 0, 0, TimeSpan.Zero));
    private readonly ZipSaveBackupStore _store;

    public ZipSaveBackupStoreTests()
    {
        _store = new ZipSaveBackupStore(_fileSystem, Options.Create(new DaoReworkOptions { AppDataDirectory = _appData }), _clock);
    }

    [Fact]
    public async Task CreateAsync_WritesEveryFileAndAManifestAndLeavesTheSourcesAlone()
    {
        var files = new[] { Save(@"Aeducan\Saves\Slot_1\Slot_1.das", "save one"), Save(@"Aeducan\Saves\Slot_1\screen.dds", "picture") };

        var backup = (await _store.CreateAsync("Default", files)).Value;

        Assert.StartsWith(Path.Combine(_appData, "save-backups", "Default", "20260929-200000-000-"), backup.Path, StringComparison.Ordinal);
        Assert.Equal(_clock.GetUtcNow(), backup.CreatedAtUtc);
        var entries = ReadEntries(backup.Path);
        Assert.Equal("save one", entries["Characters/Aeducan/Saves/Slot_1/Slot_1.das"]);
        Assert.Equal("picture", entries["Characters/Aeducan/Saves/Slot_1/screen.dds"]);
        Assert.Contains("\"schemaVersion\": 1", entries["manifest.json"], StringComparison.Ordinal);
        Assert.Equal("save one", _fileSystem.File.ReadAllText(files[0].SourcePath));
        Assert.Empty(_fileSystem.Directory.GetFiles(Path.GetDirectoryName(backup.Path)!, "*.tmp"));
    }

    [Fact]
    public async Task CreateAsync_SourceGoneMidway_FailsAndLeavesNothingBehind()
    {
        var files = new[] { Save(@"A\Saves\Slot_1\Slot_1.das", "x"), new SaveFile(Path.Combine(_saves, "gone.das"), "gone.das", 1) };

        var result = await _store.CreateAsync("Default", files);

        Assert.Equal(DiagnosticCodes.SaveBackupFailed, result.Error!.Code);
        Assert.Empty(_fileSystem.Directory.GetFiles(Path.Combine(_appData, "save-backups", "Default")));
    }

    [Fact]
    public async Task ListAsync_NewestFirstIgnoringForeignFiles()
    {
        var file = new[] { Save(@"A\Saves\Slot_1\Slot_1.das", "x") };
        var first = (await _store.CreateAsync("Default", file)).Value;
        _clock.Advance(TimeSpan.FromMinutes(5));
        var second = (await _store.CreateAsync("Default", file)).Value;
        _fileSystem.AddFile(Path.Combine(_appData, "save-backups", "Default", "notes.zip"), new MockFileData("not a backup"));
        await _store.CreateAsync("Other", file);

        var listed = await _store.ListAsync("Default");

        Assert.Equal([second.Path, first.Path], listed.Select(b => b.Path));
    }

    [Fact]
    public async Task DeleteAsync_OutsideTheStore_IsRefused()
    {
        var save = Save(@"A\Saves\Slot_1\Slot_1.das", "precious");

        await Assert.ThrowsAsync<InvalidOperationException>(() => _store.DeleteAsync(new SaveBackup(save.SourcePath, DateTimeOffset.UnixEpoch, 1)));
        Assert.True(_fileSystem.File.Exists(save.SourcePath));
    }

    [Fact]
    public async Task DeleteAsync_ABackup_RemovesIt()
    {
        var backup = (await _store.CreateAsync("Default", [Save(@"A\Saves\Slot_1\Slot_1.das", "x")])).Value;

        await _store.DeleteAsync(backup);

        Assert.Empty(await _store.ListAsync("Default"));
    }

    private SaveFile Save(string relative, string content)
    {
        var relativePath = relative.Replace('\\', Path.DirectorySeparatorChar);
        var path = Path.Combine(_saves, relativePath);
        _fileSystem.AddFile(path, new MockFileData(content));
        return new SaveFile(path, relativePath, content.Length);
    }

    private Dictionary<string, string> ReadEntries(string zipPath)
    {
        using var zip = new ZipArchive(_fileSystem.File.OpenRead(zipPath), ZipArchiveMode.Read);
        return zip.Entries.ToDictionary(e => e.FullName, e =>
        {
            using var reader = new StreamReader(e.Open());
            return reader.ReadToEnd();
        });
    }
}
