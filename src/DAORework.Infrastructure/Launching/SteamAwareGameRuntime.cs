using DAORework.Application.Abstractions;
using DAORework.Application.Launching;
using DAORework.Core.Common;
using DAORework.Core.Launching;
using DAORework.Core.Profiles;
using DAORework.Infrastructure.Installations.Steam;
using Microsoft.Extensions.Logging;

namespace DAORework.Infrastructure.Launching;

/// <summary>
/// Runs the original game (ADR-0022): a Steam install is started by asking Steam, so Steam's own checks pass; any other
/// install is started by <see cref="ProcessGameRuntime"/>, as before.
/// </summary>
public sealed class SteamAwareGameRuntime : IGameRuntime
{
    private readonly SteamGameInstall _steamInstall;
    private readonly IUriLauncher _uriLauncher;
    private readonly ProcessGameRuntime _processRuntime;
    private readonly ILogger<SteamAwareGameRuntime> _logger;

    public SteamAwareGameRuntime(
        SteamGameInstall steamInstall,
        IUriLauncher uriLauncher,
        ProcessGameRuntime processRuntime,
        ILogger<SteamAwareGameRuntime> logger)
    {
        _steamInstall = steamInstall;
        _uriLauncher = uriLauncher;
        _processRuntime = processRuntime;
        _logger = logger;
    }

    public Task<Result<LaunchResult>> LaunchAsync(RuntimeProfile profile, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);

        if (!_steamInstall.IsSteamInstall(profile.InstallDirectory))
        {
            return _processRuntime.LaunchAsync(profile, cancellationToken);
        }

        var link = SteamLink(profile.LaunchArguments);
        var opened = _uriLauncher.Open(link);
        if (opened.IsFailure)
        {
            return Task.FromResult(Result.Failure<LaunchResult>(opened.Error!));
        }

        _logger.LogInformation("Asked Steam to start Dragon Age: Origins through {Link}", link);
        return Task.FromResult(Result.Success(new LaunchResult(null, link.AbsoluteUri)));
    }

    /// <summary><c>steam://rungameid/47810</c>, or <c>steam://run/47810//&lt;arguments&gt;/</c> when there are arguments.</summary>
    public static Uri SteamLink(IReadOnlyList<string> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        return arguments.Count == 0
            ? new Uri($"steam://rungameid/{SteamLibraryLocator.AppId}")
            : new Uri($"steam://run/{SteamLibraryLocator.AppId}//{Uri.EscapeDataString(string.Join(' ', arguments))}/");
    }
}
