using System.IO.Abstractions.TestingHelpers;
using System.Security.Cryptography;
using System.Text;
using DAORework.Application.Abstractions;
using DAORework.Application.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Backups;
using DAORework.Infrastructure.Mods;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace DAORework.Infrastructure.Tests.Mods;

public sealed class FolderDeploymentBackupStoreTests
{
    private const string Hair = "packages/core/override/DAORework/hair/hair.dds";
    private const string Eyes = "packages/core/override/DAORework/hair/eyes.dds";
    private const string Old = "packages/core/override/DAORework/old/old.gda";
    private const string AddInsList = "Settings/AddIns.xml";

    private static readonly string _appData = MockUnixSupport.Path(@"C:\AppData\DAORework");
    private static readonly string _documents = MockUnixSupport.Path(@"C:\Users\Me\Documents\BioWare\Dragon Age");

    private readonly MockFileSystem _fileSystem = new();
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 9, 29, 22, 0, 0, TimeSpan.Zero));
    private readonly FolderDeploymentBackupStore _store;

    public FolderDeploymentBackupStoreTests()
    {
        var options = Options.Create(new DaoReworkOptions { AppDataDirectory = _appData });
        _store = new FolderDeploymentBackupStore(_fileSystem, options, _clock, new FolderModLibrary(_fileSystem, options, _clock));
    }

    public static TheoryData<string?, string?, bool, string> Changed => new()
    {
        { "on disk now", Sha("what the plan saw"), true, "changed after the plan was made" },
        { "appeared", null, true, "appeared after the plan was made" },
        { null, Sha("gone"), false, "disappeared after the plan was made" },
    };

    /// <summary>The memento's whole job: back up, apply a plan by hand, restore, and the folder is exactly as it was.</summary>
    [Fact]
    public async Task SaveApplyRestore_PutsTheDocumentsFolderBackByteForByte()
    {
        Write(Hair, "hair v1");
        Write(Old, "old mod");
        Write(AddInsList, "<AddInsList />");
        Write("packages/core/override/My Mods/mine.dds", "the user's own");
        var before = Snapshot();

        var backup = (await _store.SaveAsync(_documents, [
            new BackupTarget(Hair, Sha("hair v1"), Sha("hair v2")),
            new BackupTarget(Eyes, null, Sha("eyes")),
            new BackupTarget(Old, Sha("old mod"), null),
            new BackupTarget(AddInsList, null, null, AnyContent: true),
        ])).Value;

        Write(Hair, "hair v2");
        Write(Eyes, "eyes");
        _fileSystem.File.Delete(Full(Old));
        Write(AddInsList, "<AddInsList><AddInItem UID=\"x\" /></AddInsList>");

        var report = (await _store.RestoreAsync(backup)).Value;

        Assert.Equal(before, Snapshot());
        Assert.Equal(3, report.Restored);
        Assert.Equal(1, report.Removed);
        Assert.Empty(report.LeftInPlace);
    }

    [Fact]
    public async Task SaveAsync_StoresVerifiedCopiesAndAManifestAndIsListed()
    {
        Write(Hair, "hair v1");

        var backup = (await _store.SaveAsync(_documents, [new BackupTarget(Hair, Sha("hair v1"), Sha("hair v2"))])).Value;

        Assert.StartsWith("20260929-220000-000-", backup.Id, StringComparison.Ordinal);
        var entry = Assert.Single(backup.Entries);
        Assert.Equal(Sha("hair v1"), entry.BeforeSha256);
        var folder = Path.Combine(_appData, "deployment-backups", DocumentsKey.For(_documents), backup.Id);
        Assert.Equal("hair v1", _fileSystem.File.ReadAllText(Path.Combine(folder, "files", "00000")));
        Assert.Contains($"\"schemaVersion\": {DeploymentBackup.CurrentSchemaVersion}", _fileSystem.File.ReadAllText(Path.Combine(folder, "manifest.json")), StringComparison.Ordinal);
        Assert.Equal(backup.Id, Assert.Single(await _store.ListAsync(_documents)).Id);
        Assert.Equal("hair v1", _fileSystem.File.ReadAllText(Full(Hair)));
    }

    [Theory]
    [MemberData(nameof(Changed))]
    public async Task SaveAsync_FileNotAsThePlanSawIt_FailsAndLeavesNoBackup(string? content, string? expected, bool exists, string message)
    {
        if (exists)
        {
            Write(Hair, content!);
        }

        var result = await _store.SaveAsync(_documents, [new BackupTarget(Hair, expected, null)]);

        Assert.Equal(DiagnosticCodes.ModBackupFailed, result.Error!.Code);
        Assert.Contains(message, result.Error.Message, StringComparison.Ordinal);
        Assert.Empty(await _store.ListAsync(_documents));
        Assert.Empty(_fileSystem.Directory.GetDirectories(Path.Combine(_appData, "deployment-backups", DocumentsKey.For(_documents))));
    }

    [Fact]
    public async Task SaveAsync_LockedFile_FailsCleanly()
    {
        _fileSystem.AddFile(Full(Hair), new MockFileData("hair v1") { AllowedFileShare = FileShare.None });

        var result = await _store.SaveAsync(_documents, [new BackupTarget(Hair, Sha("hair v1"), null)]);

        Assert.Equal(DiagnosticCodes.ModBackupFailed, result.Error!.Code);
        Assert.Empty(await _store.ListAsync(_documents));
    }

    [Fact]
    public async Task SaveAsync_PathOutsideTheDocumentsFolder_IsRefused()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => _store.SaveAsync(_documents, [new BackupTarget("../../escape.txt", null, null)]));
    }

    [Fact]
    public async Task RestoreAsync_AddInsListThatDidNotExist_IsRemovedAgain()
    {
        var backup = (await _store.SaveAsync(_documents, [new BackupTarget(AddInsList, null, null, AnyContent: true)])).Value;
        Write(AddInsList, "<AddInsList />");

        var report = (await _store.RestoreAsync(backup)).Value;

        Assert.False(_fileSystem.File.Exists(Full(AddInsList)));
        Assert.Equal(1, report.Removed);
    }

    [Fact]
    public async Task RestoreAsync_CreatedFileTheUserChangedSince_IsLeftAlone()
    {
        var backup = (await _store.SaveAsync(_documents, [new BackupTarget(Eyes, null, Sha("eyes"))])).Value;
        Write(Eyes, "the user's edit");

        var report = (await _store.RestoreAsync(backup)).Value;

        Assert.Equal([Eyes], report.LeftInPlace);
        Assert.Equal("the user's edit", _fileSystem.File.ReadAllText(Full(Eyes)));
    }

    [Fact]
    public async Task RestoreAsync_TwiceInARow_GivesTheSameFolder()
    {
        Write(Hair, "hair v1");
        var backup = (await _store.SaveAsync(_documents, [new BackupTarget(Hair, Sha("hair v1"), Sha("hair v2"))])).Value;
        Write(Hair, "hair v2");

        await _store.RestoreAsync(backup);
        var once = Snapshot();
        var again = await _store.RestoreAsync(backup);

        Assert.True(again.IsSuccess);
        Assert.Equal(once, Snapshot());
    }

    [Fact]
    public async Task RestoreAsync_BackupFolderGone_IsNotFound()
    {
        var backup = new DeploymentBackup("20260101-000000-000-dead", _clock.GetUtcNow(), _documents, []);

        Assert.Equal(DiagnosticCodes.ModBackupNotFound, (await _store.RestoreAsync(backup)).Error!.Code);
    }

    [Fact]
    public async Task RestoreAsync_TargetLocked_FailsAndKeepsTheBackup()
    {
        Write(Hair, "hair v1");
        var backup = (await _store.SaveAsync(_documents, [new BackupTarget(Hair, Sha("hair v1"), Sha("hair v2"))])).Value;
        _fileSystem.RemoveFile(Full(Hair));
        _fileSystem.AddFile(Full(Hair), new MockFileData("hair v2") { AllowedFileShare = FileShare.None });
        _fileSystem.AddFile(Full(Hair) + ".daorework-restore", new MockFileData("stale") { AllowedFileShare = FileShare.None });

        var result = await _store.RestoreAsync(backup);

        Assert.Equal(DiagnosticCodes.ModRestoreFailed, result.Error!.Code);
        Assert.Single(await _store.ListAsync(_documents));
    }

    [Fact]
    public async Task ListAsync_IgnoresUnfinishedBackupsAndSortsNewestFirst()
    {
        Write(Hair, "hair v1");
        var first = (await _store.SaveAsync(_documents, [new BackupTarget(Hair, Sha("hair v1"), null)])).Value;
        _clock.Advance(TimeSpan.FromMinutes(1));
        var second = (await _store.SaveAsync(_documents, [new BackupTarget(Hair, Sha("hair v1"), null)])).Value;
        var folder = Path.Combine(_appData, "deployment-backups", DocumentsKey.For(_documents));
        _fileSystem.Directory.CreateDirectory(Path.Combine(folder, "20270101-000000-000-beef.tmp"));
        _fileSystem.Directory.CreateDirectory(Path.Combine(folder, "20270101-000000-000-cafe"));

        Assert.Equal([second.Id, first.Id], (await _store.ListAsync(_documents)).Select(b => b.Id));
        Assert.Empty(await _store.ListAsync(MockUnixSupport.Path(@"C:\Elsewhere")));
    }

    private static string Sha(string content) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(content)));

    private static string Full(string relative) => Path.Combine(_documents, relative.Replace('/', Path.DirectorySeparatorChar));

    private void Write(string relative, string content)
    {
        _fileSystem.Directory.CreateDirectory(Path.GetDirectoryName(Full(relative))!);
        _fileSystem.File.WriteAllText(Full(relative), content);
    }

    private List<string> Snapshot() =>
        _fileSystem.Directory.EnumerateFiles(_documents, "*", SearchOption.AllDirectories)
            .Select(f => $"{Path.GetRelativePath(_documents, f)}={_fileSystem.File.ReadAllText(f)}")
            .Order(StringComparer.Ordinal)
            .ToList();
}
