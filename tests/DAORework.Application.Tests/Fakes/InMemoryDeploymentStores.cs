using DAORework.Application.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Apply;
using DAORework.Core.Mods.Planning;

namespace DAORework.Application.Tests.Fakes;

/// <summary>An in-memory journal, record store and writer that logs steps and can fail one.</summary>
internal sealed class InMemoryDeploymentStores : IApplyJournalStore, IDeploymentRecordStore, IDeploymentWriter
{
    public ApplyJournal? Journal { get; private set; }

    public HashSet<int> Done { get; } = [];

    public DeploymentRecord? Saved { get; private set; }

    public List<string> Written { get; } = [];

    public int? FailAtCall { get; set; }

    public Task StartAsync(ApplyJournal journal, CancellationToken cancellationToken = default)
    {
        Journal = journal;
        Done.Clear();
        return Task.CompletedTask;
    }

    public Task MarkDoneAsync(string documentsDirectory, int step, CancellationToken cancellationToken = default)
    {
        Done.Add(step);
        return Task.CompletedTask;
    }

    public Task<ApplyProgress?> LoadAsync(string documentsDirectory, CancellationToken cancellationToken = default) =>
        Task.FromResult(Journal is null ? null : new ApplyProgress(Journal, Done.ToHashSet()));

    public Task DeleteAsync(string documentsDirectory, CancellationToken cancellationToken = default)
    {
        Journal = null;
        return Task.CompletedTask;
    }

    Task<Result<DeploymentRecord>> IDeploymentRecordStore.LoadAsync(string documentsDirectory, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success(Saved ?? DeploymentRecord.Empty));

    public Task SaveAsync(string documentsDirectory, DeploymentRecord record, CancellationToken cancellationToken = default)
    {
        Saved = record;
        return Task.CompletedTask;
    }

    public Task<Result> ApplyAsync(string documentsDirectory, PlanOperation operation, CancellationToken cancellationToken = default) =>
        Task.FromResult(Step($"{operation.Action} {operation.TargetPath}"));

    public Task<Result> EditAddInsListAsync(string documentsDirectory, IReadOnlyList<AddInsListChange> changes, CancellationToken cancellationToken = default) =>
        Task.FromResult(Step("AddIns.xml"));

    private Result Step(string what)
    {
        if (FailAtCall == Written.Count)
        {
            FailAtCall = null;
            return Result.Failure(new Error(DiagnosticCodes.ModApplyFailed, $"{what} is locked"));
        }

        Written.Add(what);
        return Result.Success();
    }
}
