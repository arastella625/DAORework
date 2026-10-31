using System.IO.Abstractions.TestingHelpers;
using System.IO.Compression;
using DAORework.Application.Common;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods;
using DAORework.Core.Profiles;
using DAORework.Core.Support;
using DAORework.Infrastructure.Persistence;
using DAORework.Infrastructure.Support;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace DAORework.Infrastructure.Tests.Persistence;

/// <summary>
/// Profiles, mod profiles, reports and support bundles go through the injected <c>IFileSystem</c> (SDD §8.5), so they
/// can be read back from a <see cref="MockFileSystem"/> and never touch the real disk.
/// </summary>
public sealed class PersistenceFileSystemTests
{
    private static readonly DateTimeOffset _now = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly string _appData = Path.Combine(MockUnixSupport.Path(@"C:\NotOnDisk"), Guid.NewGuid().ToString("N"), "DAORework");
    private readonly MockFileSystem _fileSystem = new();

    private DaoReworkOptions Paths => new() { AppDataDirectory = _appData };

    [Fact]
    public async Task Profiles_AreSavedListedAndLoadedThroughTheFileSystem()
    {
        var repository = new JsonProfileRepository(_fileSystem, Options.Create(Paths));

        await repository.SaveAsync(new RuntimeProfile { Name = "Mock" });

        Assert.Equal(["Mock"], await repository.ListNamesAsync());
        Assert.Equal("Mock", (await repository.LoadAsync("Mock"))!.Name);
        Assert.True(_fileSystem.File.Exists(Path.Combine(Paths.ProfilesDirectory, "Mock.json")));
        Assert.Empty(_fileSystem.Directory.GetFiles(Paths.ProfilesDirectory, "*.tmp"));
        AssertNothingOnTheRealDisk();
    }

    [Fact]
    public async Task ModProfiles_AreSavedAndListedThroughTheFileSystem()
    {
        var service = new JsonDaoModProfileService(_fileSystem, Options.Create(Paths));

        await service.SaveAsync(new DaoModProfile { Name = "Textures" });

        Assert.Equal("Textures", Assert.Single(await service.ListAsync()).Name);
        AssertNothingOnTheRealDisk();
    }

    [Fact]
    public async Task Reports_AreWrittenThroughTheFileSystem()
    {
        var path = await new ReportFileWriter(_fileSystem, new FakeTimeProvider(_now)).WriteAsync(Paths.ReportsDirectory, "dao-mock", new { Value = 1 });

        Assert.Contains("\"value\": 1", _fileSystem.File.ReadAllText(path), StringComparison.Ordinal);
        AssertNothingOnTheRealDisk();
    }

    [Fact]
    public async Task SupportBundles_AreWrittenAndReadThroughTheFileSystem()
    {
        _fileSystem.AddFile(Path.Combine(Paths.LogsDirectory, "daorework-20261001.log"), new MockFileData("started") { LastWriteTime = _now });
        var bundle = new SupportBundle(
            new RuntimeProfile { Name = "Mock" },
            new DiagnosticReport { CreatedAtUtc = _now, ProfileName = "Mock", Issues = [new DiagnosticIssue(DiagnosticSeverity.Info, DiagnosticCodes.ProfileValid, "ok")] },
            null,
            new Error(DiagnosticCodes.ModFolderUnreadable, "no mod folders in a mock"),
            PathRedactor.ForUserProfile(MockUnixSupport.Path(@"C:\Users\someone")));

        var path = await new ZipSupportBundleWriter(_fileSystem, Options.Create(Paths), new FakeTimeProvider(_now)).WriteAsync(bundle);

        await using var stream = _fileSystem.File.OpenRead(path);
        using var zip = new ZipArchive(stream, ZipArchiveMode.Read);
        Assert.Contains(zip.Entries, e => e.FullName == "logs/daorework-20261001.log");
        AssertNothingOnTheRealDisk();
    }

    private void AssertNothingOnTheRealDisk() => Assert.False(Directory.Exists(_appData), $"{_appData} was created on the real disk.");
}
