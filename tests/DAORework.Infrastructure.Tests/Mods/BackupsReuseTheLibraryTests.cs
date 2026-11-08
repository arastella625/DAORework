using System.IO.Abstractions.TestingHelpers;
using System.Security.Cryptography;
using System.Text;
using DAORework.Application.Abstractions;
using DAORework.Application.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Backups;
using DAORework.Core.Mods.Planning;
using DAORework.Infrastructure.Mods;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace DAORework.Infrastructure.Tests.Mods;

/// <summary>A backup leaves what the Library holds to the Library, and a restore proves it can get it back first (ADR-0045).</summary>
public sealed class BackupsReuseTheLibraryTests
{
    private const string Hair = "packages/core/override/DAORework/hair/hair.dds";
    private const string Eyes = "packages/core/override/DAORework/hair/eyes.dds";
    private const string Mine = "packages/core/override/DAORework/mine/mine.dds";

    private static readonly string _appData = MockUnixSupport.Path(@"C:\AppData\DAORework");
    private static readonly string _documents = MockUnixSupport.Path(@"C:\Users\Me\Documents\BioWare\Dragon Age");
    private static readonly string _download = MockUnixSupport.Path(@"C:\Get\Hair");

    private readonly MockFileSystem _fileSystem = new();
    private readonly FolderDeploymentBackupStore _store;
    private readonly FolderModLibrary _library;

    public BackupsReuseTheLibraryTests()
    {
        var options = Options.Create(new DaoReworkOptions { AppDataDirectory = _appData });
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 11, 6, 12, 0, 0, TimeSpan.Zero));
        _library = new FolderModLibrary(_fileSystem, options, clock);
        _store = new FolderDeploymentBackupStore(_fileSystem, options, clock, _library);
        _fileSystem.AddFile(Path.Combine(_download, "hair.dds"), new MockFileData("hair"));
        _fileSystem.AddFile(Path.Combine(_download, "eyes.dds"), new MockFileData("eyes"));
    }

    [Fact]
    public async Task AFileTheLibraryHolds_IsNotCopied_AndComesBackByteForByte()
    {
        var backup = await TurnOffHairAsync();

        var entry = Assert.Single(backup.Entries, e => e.TargetPath == Hair);
        Assert.Null(entry.StoredAs);
        Assert.Equal(Sha("hair"), entry.BeforeSha256);
        Assert.Empty(_fileSystem.Directory.GetFiles(Path.Combine(BackupFolder(backup), "files")));

        var restored = await _store.RestoreAsync(Assert.Single(await _store.ListAsync(_documents)));

        Assert.True(restored.IsSuccess, restored.Error?.Message);
        Assert.Equal(2, restored.Value.Restored);
        Assert.Equal("hair", _fileSystem.File.ReadAllText(Full(Hair)));
        Assert.Equal("eyes", _fileSystem.File.ReadAllText(Full(Eyes)));
    }

    [Fact]
    public async Task AFileThePlayerPutThere_IsCopied_EvenWhenAModHoldsTheSameBytes()
    {
        await _library.ImportAsync(_download);
        Write(Mine, "hair");
        var record = new DeploymentRecord([], [], []);

        var backup = (await _store.SaveAsync(_documents, [new BackupTarget(Mine, Sha("hair"), null)], record)).Value;

        Assert.NotNull(Assert.Single(backup.Entries).StoredAs);
    }

    [Fact]
    public async Task AFileThatIsNotInTheLibrary_IsCopied()
    {
        Write(Mine, "made here");
        var record = new DeploymentRecord(["combined"], [new RecordedFile(Mine, Sha("made here"), "combined")], []);

        var backup = (await _store.SaveAsync(_documents, [new BackupTarget(Mine, Sha("made here"), null)], record)).Value;

        Assert.NotNull(Assert.Single(backup.Entries).StoredAs);
    }

    [Fact]
    public async Task ADamagedLibraryCopy_MeansTheBackupKeepsItsOwn()
    {
        await _library.ImportAsync(_download);
        var (targets, record) = Deployed();
        _fileSystem.File.WriteAllText(LibraryFile("hair.dds"), "damaged");

        var backup = (await _store.SaveAsync(_documents, targets, record)).Value;

        Assert.NotNull(Assert.Single(backup.Entries, e => e.TargetPath == Hair).StoredAs);
        Assert.Null(Assert.Single(backup.Entries, e => e.TargetPath == Eyes).StoredAs);
    }

    [Fact]
    public async Task AFileThatChangedSinceThePlan_StopsTheBackup()
    {
        await _library.ImportAsync(_download);
        var (targets, record) = Deployed();
        Write(Hair, "edited since");

        var result = await _store.SaveAsync(_documents, targets, record);

        Assert.Equal(DiagnosticCodes.ModBackupFailed, result.Error!.Code);
        Assert.Contains("changed after the plan was made", result.Error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ALibraryCopyGoneOrDamaged_StopsTheRestoreBeforeAnythingChanges(bool gone)
    {
        var backup = await TurnOffHairAsync();
        Write(Hair, "what is there now");
        if (gone)
        {
            _fileSystem.File.Delete(LibraryFile("eyes.dds"));
        }
        else
        {
            _fileSystem.File.WriteAllText(LibraryFile("eyes.dds"), "damaged");
        }

        var result = await _store.RestoreAsync(backup);

        Assert.Equal(DiagnosticCodes.ModUndoNeedsRemovedMod, result.Error!.Code);
        Assert.Contains("eyes.dds is gone", result.Error.Message, StringComparison.Ordinal);
        Assert.Contains("remove it first", result.Error.Recommendation, StringComparison.Ordinal);
        Assert.Equal("what is there now", _fileSystem.File.ReadAllText(Full(Hair)));
        Assert.False(_fileSystem.File.Exists(Full(Eyes)));
        Assert.Single(await _store.ListAsync(_documents));
    }

    [Fact]
    public async Task ASavedCopyThatIsDamaged_NeverTakesThePlaceOfWhatIsThere()
    {
        Write(Mine, "before");
        var backup = (await _store.SaveAsync(_documents, [new BackupTarget(Mine, Sha("before"), Sha("after"))])).Value;
        Write(Mine, "after");
        _fileSystem.File.WriteAllText(Path.Combine(BackupFolder(backup), "files", "00000"), "damaged");

        var result = await _store.RestoreAsync(backup);

        Assert.Equal(DiagnosticCodes.ModRestoreFailed, result.Error!.Code);
        Assert.Equal("after", _fileSystem.File.ReadAllText(Full(Mine)));
    }

    [Fact]
    public async Task RestoringTwice_LeavesTheSameFiles()
    {
        var backup = await TurnOffHairAsync();

        await _store.RestoreAsync(backup);
        var again = await _store.RestoreAsync(backup);

        Assert.True(again.IsSuccess, again.Error?.Message);
        Assert.Equal("hair", _fileSystem.File.ReadAllText(Full(Hair)));
    }

    private static string Sha(string content) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(content)));

    private async Task<DeploymentBackup> TurnOffHairAsync()
    {
        await _library.ImportAsync(_download);
        var (targets, record) = Deployed();
        var backup = (await _store.SaveAsync(_documents, targets, record)).Value;
        _fileSystem.File.Delete(Full(Hair));
        _fileSystem.File.Delete(Full(Eyes));
        return backup;
    }

    private (IReadOnlyList<BackupTarget> Targets, DeploymentRecord Record) Deployed()
    {
        Write(Hair, "hair");
        Write(Eyes, "eyes");
        return (
            [new BackupTarget(Hair, Sha("hair"), null), new BackupTarget(Eyes, Sha("eyes"), null)],
            new DeploymentRecord(["hair"], [new RecordedFile(Hair, Sha("hair"), "hair"), new RecordedFile(Eyes, Sha("eyes"), "hair")], []));
    }

    private string LibraryFile(string name) =>
        _fileSystem.Directory.GetFiles(Path.Combine(_appData, "library"), name, SearchOption.AllDirectories).Single();

    private string BackupFolder(DeploymentBackup backup) => Path.Combine(_appData, "deployment-backups", DocumentsKey.For(_documents), backup.Id);

    private void Write(string relative, string content)
    {
        var path = Full(relative);
        _fileSystem.Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        _fileSystem.File.WriteAllText(path, content);
    }

    private string Full(string relative) => Path.Combine(_documents, relative.Replace('/', Path.DirectorySeparatorChar));
}
