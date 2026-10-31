using DAORework.Application.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Looks;

namespace DAORework.Application.Tests.Fakes;

/// <summary>Custom looks in memory.</summary>
internal sealed class FakeLookStore : ILookStore
{
    public Dictionary<string, Look> Looks { get; } = [];

    public List<Error> Unreadable { get; } = [];

    public Task<LookListing> ListAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new LookListing(Looks.Values.ToList(), Unreadable));

    public Task<Result<Look>> LoadAsync(string lookId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Looks.TryGetValue(lookId, out var look)
            ? Result.Success(look)
            : Result.Failure<Look>(new Error(DiagnosticCodes.LookNotFound, "Not found.")));

    public Task SaveAsync(Look look, CancellationToken cancellationToken = default)
    {
        Looks[look.Id] = look;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string lookId, CancellationToken cancellationToken = default)
    {
        Looks.Remove(lookId);
        return Task.CompletedTask;
    }
}
