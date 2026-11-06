using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using DAORework.Application.Mods.Library;
using DAORework.Core.Mods.Adoption;
using Microsoft.Extensions.Logging;

namespace DAORework.App.ViewModels;

/// <summary>
/// Holds back a download that looks like something already held, and asks first (ADR-0039). What is in the game and not
/// managed is offered <i>Manage it</i>, so taking over comes before adding a second copy. Names can't prove it, so the
/// player can always add it anyway.
/// </summary>
public sealed partial class LookFirstViewModel : PageViewModel
{
    private readonly LookBeforeAdding _look;

    public LookFirstViewModel(LookBeforeAdding look, ILogger<LookFirstViewModel> logger)
        : base(logger)
    {
        _look = look;
        Held.CollectionChanged += (_, _) => Changed();
    }

    /// <summary>Gets or sets how a held download is added after all: the Library's import.</summary>
    public Func<IReadOnlyList<string>, CancellationToken, Task>? AddAsync { get; set; }

    /// <summary>Gets or sets how what was found in the game is managed instead.</summary>
    public Func<IReadOnlyList<HeldDownload>, CancellationToken, Task>? ManageAsync { get; set; }

    public ObservableCollection<HeldDownloadRow> Held { get; } = [];

    public bool IsShown => Held.Count > 0;

    public bool CanManage => Held.Any(r => r.CanManage);

    /// <summary>Gets the sheet's heading.</summary>
    public string Title => Held.Count == 1 ? "You may already have this" : "You may already have these";

    /// <summary>Gets what the main button says.</summary>
    public string ManageLabel => Held.Count(r => r.CanManage) == 1 ? "Manage it" : "Manage them";

    /// <summary>Holds back what looks already held, off the calling thread, and returns the downloads to add now.</summary>
    public async Task<IReadOnlyList<string>> HoldBackAsync(IReadOnlyList<string> paths, IReadOnlyList<AdoptionCandidate> found, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(paths);
        var held = await Task.Run(() => _look.ExecuteAsync(paths, found, cancellationToken), cancellationToken);
        foreach (var download in held.Where(d => !Held.Any(r => r.Download.Path == d.Path)))
        {
            Held.Add(new HeldDownloadRow(download));
        }

        return [.. paths.Where(p => !held.Any(d => d.Path == p))];
    }

    [RelayCommand]
    private Task ManageHeldAsync(CancellationToken cancellationToken) => GuardAsync(
        async token =>
        {
            var manageable = Held.Where(r => r.CanManage).Select(r => r.Download).ToList();
            var rest = Held.Where(r => !r.CanManage).ToList();
            Held.Clear();
            foreach (var row in rest)
            {
                Held.Add(row);
            }

            if (ManageAsync is { } manage)
            {
                await manage(manageable, token);
            }
        },
        cancellationToken);

    [RelayCommand]
    private Task AddAnywayAsync(CancellationToken cancellationToken) => GuardAsync(
        async token =>
        {
            var paths = Held.Select(r => r.Download.Path).ToList();
            Held.Clear();
            if (AddAsync is { } add)
            {
                await add(paths, token);
            }
        },
        cancellationToken);

    [RelayCommand]
    private void Cancel() => Held.Clear();

    private void Changed()
    {
        OnPropertyChanged(nameof(IsShown));
        OnPropertyChanged(nameof(CanManage));
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(ManageLabel));
    }
}
