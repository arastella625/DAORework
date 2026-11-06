using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DAORework.Application.Launching;
using DAORework.Application.Mods.Changes;
using DAORework.Application.Profiles;
using Microsoft.Extensions.Logging;

namespace DAORework.App.ViewModels;

/// <summary>The launch action in the header, over the <see cref="LaunchGame"/> use case.</summary>
public sealed partial class LaunchViewModel : PageViewModel
{
    private readonly LaunchGame _launchGame;
    private readonly GetProfile _getProfile;
    private readonly RecordPlay _recordPlay;

    public LaunchViewModel(LaunchGame launchGame, GetProfile getProfile, RecordPlay recordPlay, ILogger<LaunchViewModel> logger)
        : base(logger)
    {
        _launchGame = launchGame;
        _getProfile = getProfile;
        _recordPlay = recordPlay;
    }

    /// <summary>Gets or sets what runs once a game has started, such as saying what changed since the last one.</summary>
    public Func<CancellationToken, Task>? AfterPlayAsync { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LaunchCommand))]
    public partial string? ProfileName { get; set; }

    [RelayCommand(CanExecute = nameof(CanLaunch))]
    private Task LaunchAsync(CancellationToken cancellationToken) =>
        GuardAsync(
            async token =>
            {
                if (ProfileName is null)
                {
                    return;
                }

                var result = await _launchGame.ExecuteAsync(new LaunchGameRequest(ProfileName), token);
                if (!result.TryGetValue(out var launched, out var error))
                {
                    ReportError(error);
                    return;
                }

                await NotePlayAsync(token);
                ReportSuccess(launched.StartedBySteam
                    ? "Steam was asked to start the game. If Steam is closed, it opens first."
                    : $"Launched {launched.ExecutablePath} (process {launched.ProcessId}).");
            },
            cancellationToken);

    /// <summary>Which mods this game had (ADR-0043). A profile without a documents folder has none to note.</summary>
    private async Task NotePlayAsync(CancellationToken cancellationToken)
    {
        if ((await ProfileDocuments.ForAsync(_getProfile, ProfileName, cancellationToken)).TryGetValue(out var documents, out _))
        {
            // Logged there. The game has started, and this only helps later, so it isn't the player's problem now.
            _ = await _recordPlay.ExecuteAsync(documents, cancellationToken);
            if (AfterPlayAsync is { } after)
            {
                await after(cancellationToken);
            }
        }
    }

    private bool CanLaunch() => !string.IsNullOrWhiteSpace(ProfileName);
}
