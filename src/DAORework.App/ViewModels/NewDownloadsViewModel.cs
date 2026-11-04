using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DAORework.Application.Mods.Downloads;
using Microsoft.Extensions.Logging;

namespace DAORework.App.ViewModels;

/// <summary>
/// The <i>New downloads</i> card on the Mods page (ADR-0034 §3 and §5). It looks when the window comes back into focus and
/// when the Mods page shows, off the calling thread. Looks and clicks take turns, so a look can't undo what a click just
/// noted. A file is added only on a click, through the same import as <i>Add mods</i>, and is then not offered again,
/// whatever became of it.
/// </summary>
public sealed partial class NewDownloadsViewModel : PageViewModel
{
    private readonly FindNewDownloads _find;
    private readonly SetDownloadHandled _handled;
    private Task _lastTurn = Task.CompletedTask;

    public NewDownloadsViewModel(FindNewDownloads find, SetDownloadHandled handled, NewDownloadsSettingsViewModel settings, ILogger<NewDownloadsViewModel> logger)
        : base(logger)
    {
        _find = find;
        _handled = handled;
        Settings = settings;
        Settings.AfterChangeAsync = LookAsync;
        Rows.CollectionChanged += (_, _) => Changed();
        PropertyChanged += OnPropertyChanged;
    }

    /// <summary>Gets or sets how a file is added: the Library's import, as <i>Add mods</i> does.</summary>
    public Func<IReadOnlyList<string>, CancellationToken, Task>? AddAsync { get; set; }

    public NewDownloadsSettingsViewModel Settings { get; }

    public ObservableCollection<NewDownloadRow> Rows { get; } = [];

    public bool IsShown => Rows.Count > 0 || IsError;

    /// <summary>Gets the line for Home, or null with nothing new.</summary>
    public string? HomeLine => Rows.Count == 0 ? null : $"{Counted.Of(Rows.Count, "new mod")} in your downloads";

    /// <summary>Reads the settings, then looks.</summary>
    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        await Settings.LoadAsync(cancellationToken);
        await LookAsync(cancellationToken);
    }

    /// <summary>Looks in the folder, off the calling thread, so the window stays free while archives are listed. Off, there is nothing to do.</summary>
    public Task LookAsync(CancellationToken cancellationToken = default) => InTurnAsync(
        async token =>
        {
            Rows.Clear();
            if (!Settings.Enabled)
            {
                ReportSuccess(string.Empty);
                return;
            }

            var found = await Task.Run(() => _find.ExecuteAsync(new FindNewDownloadsRequest(), token), token);
            if (found.TryGetValue(out var downloads, out var error))
            {
                ReportSuccess(string.Empty);
                foreach (var download in downloads)
                {
                    Rows.Add(new NewDownloadRow(download));
                }
            }
            else
            {
                ReportError(error);
            }
        },
        cancellationToken);

    [RelayCommand]
    private Task LookAgainAsync(CancellationToken cancellationToken) => LookAsync(cancellationToken);

    [RelayCommand]
    private Task AddOneAsync(NewDownloadRow row, CancellationToken cancellationToken) => AddRowsAsync([row], cancellationToken);

    [RelayCommand]
    private Task AddAllAsync(CancellationToken cancellationToken) => AddRowsAsync([.. Rows], cancellationToken);

    [RelayCommand]
    private Task NotThisOneAsync(NewDownloadRow row, CancellationToken cancellationToken) => InTurnAsync(token => DealtWithAsync(row, token), cancellationToken);

    /// <summary>Runs after whatever came before, always from the window's thread: a look and a click both save the list of files dealt with.</summary>
    private Task InTurnAsync(Func<CancellationToken, Task> work, CancellationToken cancellationToken) => GuardAsync(
        async token =>
        {
            var previous = _lastTurn;
            var turn = new TaskCompletionSource();
            _lastTurn = turn.Task;
            await previous;
            try
            {
                await work(token);
            }
            finally
            {
                turn.SetResult();
            }
        },
        cancellationToken);

    private Task AddRowsAsync(IReadOnlyList<NewDownloadRow> rows, CancellationToken cancellationToken) => InTurnAsync(
        async token =>
        {
            if (AddAsync is { } add)
            {
                await add([.. rows.Select(r => r.Download.File.Path)], token);
            }

            foreach (var row in rows)
            {
                await DealtWithAsync(row, token);
            }
        },
        cancellationToken);

    private async Task DealtWithAsync(NewDownloadRow row, CancellationToken cancellationToken)
    {
        var result = await _handled.ExecuteAsync(row.Download.File, cancellationToken);
        if (result.Error is { } error)
        {
            ReportError(error);
        }

        Rows.Remove(row);
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IsError))
        {
            OnPropertyChanged(nameof(IsShown));
        }
    }

    private void Changed()
    {
        OnPropertyChanged(nameof(IsShown));
        OnPropertyChanged(nameof(HomeLine));
    }
}
