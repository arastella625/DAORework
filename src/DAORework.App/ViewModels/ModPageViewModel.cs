using CommunityToolkit.Mvvm.Input;
using DAORework.Application.Catalog;
using Microsoft.Extensions.Logging;

namespace DAORework.App.ViewModels;

/// <summary><i>Open the mod's page</i> on the Mods tab (ADR-0034 §7), with a line for when it can't be opened.</summary>
public sealed partial class ModPageViewModel : PageViewModel
{
    private readonly OpenModPage _open;

    public ModPageViewModel(OpenModPage open, ILogger<ModPageViewModel> logger)
        : base(logger)
    {
        _open = open;
    }

    [RelayCommand]
    private void Open(string? link)
    {
        var result = _open.Execute(link);
        if (result.Error is { } error)
        {
            ReportError(error);
        }
        else
        {
            ReportSuccess(string.Empty);
        }
    }
}
