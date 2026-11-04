using System.IO.Abstractions;
using DAORework.Application.Abstractions;
using DAORework.Application.Launching;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Launching;
using DAORework.Core.Profiles;
using Microsoft.Extensions.Logging;

namespace DAORework.Infrastructure.Launching;

/// <summary>Runs the original game (or its official launcher) as a process, on Windows.</summary>
public sealed class ProcessGameRuntime : IGameRuntime
{
    private readonly IFileSystem _fileSystem;
    private readonly IProcessLauncher _processLauncher;
    private readonly IHostPlatform _platform;
    private readonly ILogger<ProcessGameRuntime> _logger;

    public ProcessGameRuntime(IFileSystem fileSystem, IProcessLauncher processLauncher, IHostPlatform platform, ILogger<ProcessGameRuntime> logger)
    {
        _platform = platform;
        _fileSystem = fileSystem;
        _processLauncher = processLauncher;
        _logger = logger;
    }

    public Task<Result<LaunchResult>> LaunchAsync(RuntimeProfile profile, CancellationToken cancellationToken = default)
    {
        // A Steam copy goes through Steam before it gets here. DAORework can't know which Wine or launcher runs any other (ADR-0031).
        if (_platform.IsLinux)
        {
            return Task.FromResult(Result.Failure<LaunchResult>(new Error(
                DiagnosticCodes.LaunchNotSupported,
                "DAORework can start only Steam's copy on Linux.",
                "Start the game from the launcher you run it with.")));
        }

        var useLauncher = profile.LaunchTarget == LaunchTarget.OfficialLauncher;
        var executablePath = useLauncher ? profile.LauncherExecutablePath : profile.GameExecutablePath;

        if (string.IsNullOrWhiteSpace(executablePath))
        {
            return Task.FromResult(Result.Failure<LaunchResult>(new Error(
                useLauncher ? DiagnosticCodes.LauncherExecutableMissing : DiagnosticCodes.GameExecutableMissing,
                $"No executable path is configured for launch target '{profile.LaunchTarget}'.")));
        }

        if (!_fileSystem.File.Exists(executablePath))
        {
            return Task.FromResult(Result.Failure<LaunchResult>(new Error(
                useLauncher ? DiagnosticCodes.LauncherExecutableNotFound : DiagnosticCodes.GameExecutableNotFound,
                $"The configured executable does not exist: {executablePath}")));
        }

        var request = new ProcessStartRequest(executablePath, Path.GetDirectoryName(executablePath), profile.LaunchArguments);
        var started = _processLauncher.Start(request);

        if (!started.TryGetValue(out var processId, out var error))
        {
            return Task.FromResult(Result.Failure<LaunchResult>(error));
        }

        _logger.LogInformation(
            "Started Dragon Age: Origins using {LaunchTarget} with process id {ProcessId}",
            profile.LaunchTarget,
            processId);

        return Task.FromResult(Result.Success(new LaunchResult(processId, executablePath)));
    }
}
