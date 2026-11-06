using DAORework.Application.Abstractions;
using DAORework.Application.Mods.Changes;
using DAORework.Application.Tests.Fakes;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Troubleshooting;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DAORework.Application.Tests.Mods.Changes;

public sealed class RecordPlayTests
{
    private const string Documents = "docs";

    private readonly FakeHistory _history = new();
    private readonly FakeSearches _searches = new();

    [Fact]
    public async Task Execute_AGame_IsNoted()
    {
        Assert.True((await UseCase().ExecuteAsync(Documents)).IsSuccess);

        Assert.Single(_history.Saved!.Plays);
    }

    [Fact]
    public async Task Execute_DuringASearch_NotesNothing()
    {
        _searches.Search = ProblemSearch.Start(["a", "b"]);

        await UseCase().ExecuteAsync(Documents);

        Assert.Null(_history.Saved);
    }

    [Fact]
    public async Task Execute_AHistoryThatCantBeRead_StartsAgain()
    {
        _history.Unreadable = true;

        await UseCase().ExecuteAsync(Documents);

        Assert.Single(_history.Saved!.Plays);
    }

    [Fact]
    public async Task Execute_ASaveThatFails_IsAFailure_NotAnException()
    {
        _history.SaveFails = true;

        var result = await UseCase().ExecuteAsync(Documents);

        Assert.Equal(DiagnosticCodes.PlayHistoryUnreadable, result.Error!.Code);
    }

    private RecordPlay UseCase() => new(new InMemoryDeploymentStores(), _history, _searches, TimeProvider.System, NullLogger<RecordPlay>.Instance);

    private sealed class FakeHistory : IPlayHistoryStore
    {
        public PlayHistory? Saved { get; private set; }

        public bool Unreadable { get; set; }

        public bool SaveFails { get; set; }

        public Task<Result<PlayHistory>> LoadAsync(string documentsDirectory, CancellationToken cancellationToken = default) => Task.FromResult(
            Unreadable ? Result.Failure<PlayHistory>(new Error(DiagnosticCodes.PlayHistoryUnreadable, "Broken.")) : Result.Success(PlayHistory.Empty));

        public Task SaveAsync(string documentsDirectory, PlayHistory history, CancellationToken cancellationToken = default)
        {
            if (SaveFails)
            {
                throw new IOException("Locked.");
            }

            Saved = history;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeSearches : IProblemSearchStore
    {
        public ProblemSearch? Search { get; set; }

        public Task<Result<ProblemSearch?>> LoadAsync(string documentsDirectory, CancellationToken cancellationToken = default) => Task.FromResult(Result.Success(Search));

        public Task SaveAsync(string documentsDirectory, ProblemSearch search, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public void Delete(string documentsDirectory)
        {
        }
    }
}
