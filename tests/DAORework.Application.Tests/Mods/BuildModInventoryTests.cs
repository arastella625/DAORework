using System.IO.Abstractions.TestingHelpers;
using DAORework.Application.Mods;
using DAORework.Application.Tests.Fakes;
using DAORework.Core.Common;
using DAORework.Core.Profiles;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace DAORework.Application.Tests.Mods;

public sealed class BuildModInventoryTests
{
    private static readonly DateTimeOffset _now = new(2026, 9, 29, 12, 0, 0, TimeSpan.Zero);
    private static readonly string _documents = TestPaths.Root("Docs");
    private static readonly string _override = GamePaths.OverrideDirectory(_documents);

    private readonly MockFileSystem _fileSystem = new();
    private readonly FakeFileHasher _hasher = new();

    private static RuntimeProfile Profile => new() { Name = "P", UserDocumentsDirectory = _documents };

    private BuildModInventory CreateUseCase() =>
        new(_fileSystem, _hasher, new FakeKnownFolders(), new FakeTimeProvider(_now), NullLogger<BuildModInventory>.Instance);

    [Fact]
    public async Task ExecuteAsync_NoModFolders_ReturnsEmptyInventory()
    {
        var result = await CreateUseCase().ExecuteAsync(new BuildModInventoryRequest(Profile));

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Items);
        Assert.Equal(0, result.Value.TotalFiles);
        Assert.Equal(_now, result.Value.CreatedAtUtc);
    }

    [Fact]
    public async Task ExecuteAsync_FilesInBothFolders_ListsThemSortedWithRelativePaths()
    {
        _fileSystem.AddFile(Path.Combine(_override, "zeta.erf"), new MockFileData(new byte[10]));
        _fileSystem.AddFile(Path.Combine(_override, "sub", "alpha.gda"), new MockFileData(new byte[20]));
        _fileSystem.AddFile(Path.Combine(GamePaths.AddInsDirectory(_documents), "mod", "manifest.xml"), new MockFileData("<m/>"));

        var inventory = (await CreateUseCase().ExecuteAsync(new BuildModInventoryRequest(Profile))).Value;

        Assert.Equal(3, inventory.TotalFiles);
        Assert.Equal(["AddIns", "Override", "Override"], inventory.Items.Select(i => i.SourceFolder));
        Assert.Equal(Path.Combine("sub", "alpha.gda"), inventory.Items[1].RelativePath);
        Assert.Equal(".gda", inventory.Items[1].Extension);
        Assert.Equal(20, inventory.Items[1].SizeBytes);
        Assert.All(inventory.Items, i => Assert.Null(i.Sha256Hash));
        Assert.All(inventory.Items, i => Assert.Null(i.ReadError));
        Assert.Empty(_hasher.RequestedPaths);
    }

    [Fact]
    public async Task ExecuteAsync_WithHashes_UsesTheHasherAndFlagsDuplicateNames()
    {
        _fileSystem.AddFile(Path.Combine(_override, "one", "same.gda"), new MockFileData("a"));
        _fileSystem.AddFile(Path.Combine(_override, "two", "same.gda"), new MockFileData("b"));

        var inventory = (await CreateUseCase().ExecuteAsync(new BuildModInventoryRequest(Profile, IncludeHashes: true))).Value;

        Assert.True(inventory.IncludesHashes);
        Assert.All(inventory.Items, i => Assert.Equal("HASH:same.gda", i.Sha256Hash));
        Assert.Equal(["same.gda"], inventory.DuplicateFileNames);
        Assert.Empty(inventory.DuplicateRelativePaths);
    }

    [Fact]
    public async Task ExecuteAsync_UnreadableFileWhileHashing_IsListedWithReadErrorNotDropped()
    {
        var bad = Path.Combine(_override, "locked.erf");
        _fileSystem.AddFile(bad, new MockFileData("x"));
        _fileSystem.AddFile(Path.Combine(_override, "fine.erf"), new MockFileData("y"));
        _hasher.UnreadablePaths.Add(bad);

        var inventory = (await CreateUseCase().ExecuteAsync(new BuildModInventoryRequest(Profile, IncludeHashes: true))).Value;

        Assert.Equal(2, inventory.TotalFiles);
        Assert.Equal(1, inventory.UnreadableFileCount);
        var locked = Assert.Single(inventory.Items, i => i.FileName == "locked.erf");
        Assert.Null(locked.Sha256Hash);
        Assert.Contains("unreadable", locked.ReadError, StringComparison.Ordinal);
        Assert.NotNull(Assert.Single(inventory.Items, i => i.FileName == "fine.erf").Sha256Hash);
    }
}
