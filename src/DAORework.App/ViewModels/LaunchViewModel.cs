using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DAORework.Application.Launching;
using Microsoft.Extensions.Logging;

namespace DAORework.App.ViewModels;

/// <summary>The launch action in the header, over the <see cref="LaunchGame"/> use case.</summary>
public sealed partial class LaunchViewModel : PageViewModel
{
    private readonly LaunchGame _launchGame;

    public LaunchViewModel(LaunchGame launchGame, ILogger<LaunchViewModel> logger)
        : base(logger)
    {
        _launchGame = launchGame;
    }

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

                ReportSuccess($"Launched {launched.ExecutablePath} (process {launched.ProcessId}).");
            },
            cancellationToken);

    private bool CanLaunch() => !string.IsNullOrWhiteSpace(ProfileName);
}
