using DAORework.Application.Launching;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Launching;
using DAORework.Core.Profiles;

namespace DAORework.Infrastructure.Launching;

/// <summary>Deliberately unimplemented (README: research branch, not a milestone).</summary>
public sealed class ExperimentalRuntime : IGameRuntime
{
    public Task<Result<LaunchResult>> LaunchAsync(RuntimeProfile profile, CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Failure<LaunchResult>(new Error(
            DiagnosticCodes.RuntimeExperimentalUnavailable,
            "The experimental runtime is intentionally not implemented yet.",
            "Build tools, diagnostics and clean-room research first. Set the profile's runtime mode to OriginalGame.")));
}
