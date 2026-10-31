using System.IO.Abstractions.TestingHelpers;
using DAORework.Application.Abstractions;
using DAORework.Application.Profiles;
using DAORework.Application.Saves;
using DAORework.Application.Tests.Fakes;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Profiles;
using DAORework.Core.Saves;
using Xunit;

namespace DAORework.Application.Tests.Saves;

public sealed class BackupSavesTests
{
    private static readonly string _documents = TestPaths.Root("Docs", "BioWare", "Dragon Age");

    private readonly MockFileSystem _fileSystem = new();
    private readonly InMemoryProfileRepository _repository = new();
    private readonly FakeStore _store = new();

    public BackupSavesTests()
    {
        _repository.Profiles["A"] = new RuntimeProfile { Name = "A", UserDocumentsDirectory = _documents };
    }

    [Fact]
    public async Task ExecuteAsync_NoSaves_FailsWithoutWriting()
    {
        var result = await Execute("A");

        Assert.Equal(DiagnosticCodes.SaveBackupNothingToBackUp, result.Error!.Code);
        Assert.Empty(_store.Created);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidRetention_FailsBeforeReadingAnything()
    {
        AddSave("Aeducan", "Slot_1", "Slot_1.das");

        Assert.Equal(DiagnosticCodes.SaveBackupRetentionInvalid, (await Execute("A", keep: 0)).Error!.Code);
        Assert.Empty(_store.Created);
    }

    [Fact]
    public async Task ExecuteAsync_UnknownProfile_Fails()
    {
        Assert.Equal(DiagnosticCodes.ProfileNotFound, (await Execute("Nope")).Error!.Code);
    }

    [Fact]
    public async Task ExecuteAsync_Saves_CopiesEveryFileUnderCharactersWithRelativePaths()
    {
        AddSave("Aeducan", "Slot_1", "Slot_1.das", 100);
        AddSave("Aeducan", "Slot_1", "screen.dds", 50);
        AddSave("Cousland", "Slot_2", "Slot_2.das", 10);

        var result = (await Execute("A")).Value;

        Assert.Equal(3, result.FileCount);
        Assert.Equal(160, result.SourceBytes);
        Assert.Equal(
            [Path.Combine("Aeducan", "Saves", "Slot_1", "screen.dds"), Path.Combine("Aeducan", "Saves", "Slot_1", "Slot_1.das"), Path.Combine("Cousland", "Saves", "Slot_2", "Slot_2.das")],
            Assert.Single(_store.Created).Select(f => f.RelativePath));
    }

    [Fact]
    public async Task ExecuteAsync_OverTheLimit_DeletesOnlyTheOldestAfterTheNewBackupExists()
    {
        AddSave("Aeducan", "Slot_1", "Slot_1.das");
        for (var i = 0; i < 3; i++)
        {
            await Execute("A", keep: 2);
        }

        var result = (await Execute("A", keep: 2)).Value;

        Assert.Equal(["A-1.zip"], result.Deleted.Select(b => b.Path));
        Assert.Equal(["A-2.zip", "A-3.zip"], _store.Backups.Select(b => b.Path).Order());
        Assert.Contains(result.Backup, _store.Backups);
    }

    [Fact]
    public async Task ExecuteAsync_LoweringTheLimit_TrimsEverythingOlderInOneRun()
    {
        AddSave("Aeducan", "Slot_1", "Slot_1.das");
        for (var i = 0; i < 4; i++)
        {
            await Execute("A");
        }

        var result = (await Execute("A", keep: 1)).Value;

        Assert.Equal(4, result.Deleted.Count);
        Assert.Equal([result.Backup], _store.Backups);
    }

    private Task<Result<SaveBackupResult>> Execute(string profile, int keep = SaveBackupRetention.DefaultKeepLatest) =>
        new BackupSaves(new GetProfile(_repository), _store, _fileSystem, new FakeKnownFolders()).ExecuteAsync(new BackupSavesRequest(profile, keep));

    private void AddSave(string character, string slot, string file, int bytes = 1) =>
        _fileSystem.AddFile(Path.Combine(GamePaths.CharactersDirectory(_documents), character, "Saves", slot, file), new MockFileData(new byte[bytes]));

    private sealed class FakeStore : ISaveBackupStore
    {
        private int _clock;

        public List<IReadOnlyList<SaveFile>> Created { get; } = [];

        public List<SaveBackup> Backups { get; } = [];

        public Task<Result<SaveBackup>> CreateAsync(string profileName, IReadOnlyList<SaveFile> files, CancellationToken cancellationToken = default)
        {
            Created.Add(files);
            var backup = new SaveBackup($"{profileName}-{_clock}.zip", DateTimeOffset.UnixEpoch.AddMinutes(_clock++), 1);
            Backups.Add(backup);
            return Task.FromResult(Result.Success(backup));
        }

        public Task<IReadOnlyList<SaveBackup>> ListAsync(string profileName, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SaveBackup>>(Backups.OrderByDescending(b => b.CreatedAtUtc).ToList());

        public Task DeleteAsync(SaveBackup backup, CancellationToken cancellationToken = default)
        {
            Backups.Remove(backup);
            return Task.CompletedTask;
        }
    }
}
