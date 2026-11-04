using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DAORework.App.Services;
using DAORework.Application.Mods.Downloads;
using Microsoft.Extensions.Logging;

namespace DAORework.App.ViewModels;

/// <summary>
/// Whether DAORework looks for new downloads, and where (ADR-0034 §1 and §2): the one question on Home, and <i>New
/// downloads</i> in Settings. Off until the player says yes.
/// </summary>
public sealed partial class NewDownloadsSettingsViewModel : PageViewModel
{
    private readonly GetNewDownloadsSettings _get;
    private readonly SetNewDownloads _set;
    private readonly IPathPicker _picker;

    public NewDownloadsSettingsViewModel(GetNewDownloadsSettings get, SetNewDownloads set, IPathPicker picker, ILogger<NewDownloadsSettingsViewModel> logger)
        : base(logger)
    {
        _get = get;
        _set = set;
        _picker = picker;
    }

    /// <summary>Gets or sets what runs after a change, such as looking at once.</summary>
    public Func<CancellationToken, Task>? AfterChangeAsync { get; set; }

    [ObservableProperty]
    public partial bool Enabled { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FolderText))]
    public partial string? Folder { get; set; }

    [ObservableProperty]
    public partial bool ShowsQuestion { get; set; }

    public string FolderText => Folder ?? "Your Downloads folder";

    public Task LoadAsync(CancellationToken cancellationToken = default) => GuardAsync(
        async token =>
        {
            if ((await _get.ExecuteAsync(token)).TryGetValue(out var settings, out var error))
            {
                Enabled = settings.Enabled;
                Folder = settings.Folder;
                ShowsQuestion = !settings.Asked;
            }
            else
            {
                ReportError(error);
            }
        },
        cancellationToken);

    [RelayCommand]
    private Task TurnOnAsync(CancellationToken cancellationToken) => SetAsync(enabled: true, Folder, cancellationToken);

    [RelayCommand]
    private Task TurnOffAsync(CancellationToken cancellationToken) => SetAsync(enabled: false, Folder, cancellationToken);

    [RelayCommand]
    private async Task ChooseFolderAsync(CancellationToken cancellationToken)
    {
        if (await _picker.PickFolderAsync("Choose the folder your browser saves to", cancellationToken) is { } folder)
        {
            await SetAsync(Enabled, folder, cancellationToken);
        }
    }

    private Task SetAsync(bool enabled, string? folder, CancellationToken cancellationToken) => GuardAsync(
        async token =>
        {
            if (!(await _set.ExecuteAsync(enabled, folder, token)).TryGetValue(out var settings, out var error))
            {
                ReportError(error);
                return;
            }

            Enabled = settings.Enabled;
            Folder = settings.Folder;
            ShowsQuestion = false;
            if (AfterChangeAsync is { } afterChange)
            {
                await afterChange(token);
            }
        },
        cancellationToken);
}
