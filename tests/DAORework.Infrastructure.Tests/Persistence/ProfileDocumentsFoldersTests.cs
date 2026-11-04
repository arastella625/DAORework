using System.IO.Abstractions.TestingHelpers;
using DAORework.Application.Common;
using DAORework.Application.Documents;
using DAORework.Core.Profiles;
using DAORework.Infrastructure.Persistence;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace DAORework.Infrastructure.Tests.Persistence;

/// <summary>Damaged profiles through the real repository: what it really throws must not stop detection (ADR-0032).</summary>
public sealed class ProfileDocumentsFoldersTests
{
    private static readonly string _folder = MockUnixSupport.Path(@"D:\BioWare\Dragon Age");

    private readonly MockFileSystem _fileSystem = new();
    private readonly DaoReworkOptions _options = new() { AppDataDirectory = MockUnixSupport.Path(@"C:\AppData") };
    private readonly JsonProfileRepository _profiles;

    public ProfileDocumentsFoldersTests()
    {
        _profiles = new JsonProfileRepository(_fileSystem, Options.Create(_options));
    }

    private ProfileDocumentsFolders Reader => new(_profiles, NullLogger<ProfileDocumentsFolders>.Instance);

    [Theory]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("[]")]
    [InlineData("""{"userDocumentsDirectory": 5}""")]
    [InlineData("""{"schemaVersion": 99999}""")]
    public async Task ReadAsync_ADamagedProfile_ReturnsTheOthers(string damaged)
    {
        await _profiles.SaveAsync(new RuntimeProfile { Name = "Good", UserDocumentsDirectory = _folder });
        _fileSystem.AddFile(_fileSystem.Path.Combine(_options.ProfilesDirectory, "Damaged.json"), new MockFileData(damaged));

        Assert.Equal(_folder, Assert.Single(await Reader.ReadAsync()));
    }

    [Fact]
    public async Task ReadAsync_TheProfilesFolderIsGone_ReturnsNothing()
    {
        _fileSystem.Directory.Delete(_options.ProfilesDirectory, recursive: true);

        Assert.Empty(await Reader.ReadAsync());
    }
}
