using System.IO.Abstractions;
using DAORework.Infrastructure.Persistence;
using Xunit;

namespace DAORework.Infrastructure.Tests.Persistence;

public sealed class AtomicJsonFileTests : IDisposable
{
    private readonly FileSystem _fileSystem = new();

    private readonly TempDirectory _temp = new();

    public void Dispose() => _temp.Dispose();

    [Fact]
    public async Task WriteAsync_CreatesTheFileAndLeavesNoTempFile()
    {
        Directory.CreateDirectory(_temp.Path);
        var path = _temp.File("doc.json");

        await AtomicJsonFile.WriteAsync(_fileSystem, path, new Document("one"));

        Assert.Contains("\"value\": \"one\"", await File.ReadAllTextAsync(path), StringComparison.Ordinal);
        Assert.Equal(["doc.json"], Directory.GetFiles(_temp.Path).Select(Path.GetFileName));
    }

    [Fact]
    public async Task WriteAsync_ReplacesAnExistingFile()
    {
        Directory.CreateDirectory(_temp.Path);
        var path = _temp.File("doc.json");
        await AtomicJsonFile.WriteAsync(_fileSystem, path, new Document("one"));

        await AtomicJsonFile.WriteAsync(_fileSystem, path, new Document("two"));

        Assert.Contains("\"two\"", await File.ReadAllTextAsync(path), StringComparison.Ordinal);
    }

    [Fact]
    public async Task WriteAsync_FailedSerialization_KeepsTheOldFileAndCleansUp()
    {
        Directory.CreateDirectory(_temp.Path);
        var path = _temp.File("doc.json");
        await AtomicJsonFile.WriteAsync(_fileSystem, path, new Document("one"));

        await Assert.ThrowsAnyAsync<Exception>(() => AtomicJsonFile.WriteAsync(_fileSystem, path, new Exploding()));

        Assert.Contains("\"one\"", await File.ReadAllTextAsync(path), StringComparison.Ordinal);
        Assert.Equal(["doc.json"], Directory.GetFiles(_temp.Path).Select(Path.GetFileName));
    }

    [Fact]
    public async Task WriteAsync_WithoutOverwrite_RefusesAnExistingFile()
    {
        Directory.CreateDirectory(_temp.Path);
        var path = _temp.File("doc.json");
        await AtomicJsonFile.WriteAsync(_fileSystem, path, new Document("one"));

        await Assert.ThrowsAsync<IOException>(() => AtomicJsonFile.WriteAsync(_fileSystem, path, new Document("two"), overwrite: false));

        Assert.Contains("\"one\"", await File.ReadAllTextAsync(path), StringComparison.Ordinal);
        Assert.Equal(["doc.json"], Directory.GetFiles(_temp.Path).Select(Path.GetFileName));
    }

    private sealed record Document(string Value);

    private sealed class Exploding
    {
        public string Value => throw new InvalidOperationException("boom");
    }
}
