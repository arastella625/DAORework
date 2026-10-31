using DAORework.Application.Abstractions;
using DAORework.Application.Diagnostics;
using DAORework.Application.Profiles;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Launching;
using Microsoft.Extensions.Logging;

namespace DAORework.Application.Launching;

/// <summary>
/// Loads a profile, validates it, writes the diagnostic report, and starts the game through
/// the runtime for the profile's mode. It writes nothing but the report (audit C8).
/// </summary>
public sealed class LaunchGame
{
    private readonly IProfileRepository _profileStore;
    private readonly RunDiagnostics _runDiagnostics;
    private readonly IDiagnosticReportWriter _reportWriter;
    private readonly IGameRuntimeResolver _runtimeResolver;
    private readonly ILogger<LaunchGame> _logger;

    public LaunchGame(
        IProfileRepository profileStore,
        RunDiagnostics runDiagnostics,
        IDiagnosticReportWriter reportWriter,
        IGameRuntimeResolver runtimeResolver,
        ILogger<LaunchGame> logger)
    {
        _profileStore = profileStore;
        _runDiagnostics = runDiagnostics;
        _reportWriter = reportWriter;
        _runtimeResolver = runtimeResolver;
        _logger = logger;
    }

    public async Task<Result<LaunchResult>> ExecuteAsync(LaunchGameRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var profile = await _profileStore.LoadAsync(request.ProfileName, cancellationToken);
        if (profile is null)
        {
            return Result.Failure<LaunchResult>(new Error(
                DiagnosticCodes.ProfileNotFound,
                $"Profile '{request.ProfileName}' was not found.",
                $"Run: init {request.ProfileName}"));
        }

        var report = (await _runDiagnostics.ExecuteAsync(new RunDiagnosticsRequest(profile), cancellationToken)).Value;
        var reportPath = await _reportWriter.WriteAsync(report, cancellationToken);

        if (!report.CanLaunch)
        {
            return Result.Failure<LaunchResult>(new Error(
                DiagnosticCodes.ProfileValidationFailed,
                $"Profile '{profile.Name}' failed launch validation.",
                $"Review the diagnostic report: {reportPath}"));
        }

        var runtime = _runtimeResolver.Resolve(profile.RuntimeMode);
        if (runtime is null)
        {
            return Result.Failure<LaunchResult>(new Error(
                DiagnosticCodes.RuntimeModeUnsupported,
                $"No runtime is registered for mode '{profile.RuntimeMode}'.",
                "Set the profile's runtime mode to OriginalGame."));
        }

        var launched = await runtime.LaunchAsync(profile, cancellationToken);

        if (launched.TryGetValue(out var result, out _))
        {
            _logger.LogInformation(
                "Launched profile {ProfileName} in mode {RuntimeMode} with process id {ProcessId}",
                profile.Name,
                profile.RuntimeMode,
                result.ProcessId);
        }

        return launched;
    }
}
