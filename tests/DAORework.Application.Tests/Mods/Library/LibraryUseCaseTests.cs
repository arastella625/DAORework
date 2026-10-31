using DAORework.Application.Abstractions;
using DAORework.Application.Mods.Library;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Library;
using Xunit;

namespace DAORework.Application.Tests.Mods.Library;

public sealed class LibraryUseCaseTests
{
    private readonly FakeLibrary _library = new();

    [Fact]
    public async Task Import_PassesThePathToTheLibrary()
    {
        var result = await new ImportMod(_library).ExecuteAsync(new ImportModRequest("C:/Downloads/dog.dazip"));

        Assert.True(result.IsSuccess);
        Assert.Equal("C:/Downloads/dog.dazip", _library.Imported);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public async Task Import_NoPath_FailsWithoutTouchingTheLibrary(string path)
    {
        var result = await new ImportMod(_library).ExecuteAsync(new ImportModRequest(path));

        Assert.Equal(DiagnosticCodes.ModImportFailed, result.Error!.Code);
        Assert.Null(_library.Imported);
    }

    [Fact]
    public async Task List_ReturnsTheLibrary()
    {
        Assert.Single((await new ListLibrary(_library).ExecuteAsync()).Value);
    }

    private sealed class FakeLibrary : IModLibrary
    {
        private static readonly LibraryEntry _entry = new(new LibraryMod("dog-1", "Dog", ModKind.AddIn, [], "uid"), "dog.dazip", "abc", DateTimeOffset.UnixEpoch);

        public string? Imported { get; private set; }

        public Task<Result<LibraryImport>> ImportAsync(string sourcePath, CancellationToken cancellationToken = default)
        {
            Imported = sourcePath;
            return Task.FromResult(Result.Success(new LibraryImport(_entry, AlreadyInLibrary: false)));
        }

        public Task<IReadOnlyList<LibraryEntry>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<LibraryEntry>>([_entry]);
    }
}
