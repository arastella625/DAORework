using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DAORework.Application.Saves;
using DAORework.Core.Common;
using DAORework.Core.Saves;
using Microsoft.Extensions.Logging;

namespace DAORework.App.ViewModels;

/// <summary>The saves page: back up the profile's saves now, choose how many backups to keep, and see the existing ones.</summary>
public sealed partial class SavesPageViewModel : PageViewModel
{
    private readonly BackupSaves _backupSaves;
    private readonly ListSaveBackups _listSaveBackups;

    public SavesPageViewModel(BackupSaves backupSaves, ListSaveBackups listSaveBackups, ILogger<SavesPageViewModel> logger)
        : base(logger)
    {
        _backupSaves = backupSaves;
        _listSaveBackups = listSaveBackups;
        KeepLatest = SaveBackupRetention.DefaultKeepLatest;
    }

    public ObservableCollection<SaveBackup> Backups { get; } = [];

    [ObservableProperty]
    public partial string? ProfileName { get; set; }

    /// <summary>Gets or sets how many backups to keep; decimal because that is what a NumericUpDown binds to.</summary>
    [ObservableProperty]
    public partial decimal? KeepLatest { get; set; }

    public Task LoadAsync(string? profileName, CancellationToken cancellationToken = default)
    {
        ProfileName = profileName;
        Backups.Clear();
        ReportSuccess(string.Empty);
        return profileName is null ? Task.CompletedTask : RefreshAsync(cancellationToken);
    }

    [RelayCommand]
    private Task BackUpAsync(CancellationToken cancellationToken) =>
        GuardAsync(
            async token =>
            {
                if (ProfileName is null)
                {
                    return;
                }

                var request = new BackupSavesRequest(ProfileName, (int)(KeepLatest ?? SaveBackupRetention.DefaultKeepLatest));
                if (!(await _backupSaves.ExecuteAsync(request, token)).TryGetValue(out var result, out var error))
                {
                    ReportError(error);
                    return;
                }

                await ReloadAsync(token);
                var removed = result.Deleted.Count == 0 ? string.Empty : $" Removed {result.Deleted.Count} older backup(s).";
                ReportSuccess($"Backed up {result.FileCount} file(s), {new ByteSize(result.SourceBytes)}.{removed}");
            },
            cancellationToken);

    [RelayCommand]
    private Task RefreshAsync(CancellationToken cancellationToken) => GuardAsync(ReloadAsync, cancellationToken);

    private async Task ReloadAsync(CancellationToken cancellationToken)
    {
        if (ProfileName is null)
        {
            return;
        }

        if (!(await _listSaveBackups.ExecuteAsync(new ListSaveBackupsRequest(ProfileName), cancellationToken)).TryGetValue(out var backups, out var error))
        {
            ReportError(error);
            return;
        }

        Backups.Clear();
        foreach (var backup in backups)
        {
            Backups.Add(backup);
        }
    }
}
