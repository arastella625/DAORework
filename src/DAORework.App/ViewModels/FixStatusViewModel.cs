using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DAORework.Application.Fixes;
using DAORework.Application.Mods.Apply;
using DAORework.Application.Mods.Changes;
using DAORework.Application.Profiles;
using DAORework.Core.Common;
using DAORework.Core.Fixes;
using DAORework.Core.Mods.Backups;
using Microsoft.Extensions.Logging;

namespace DAORework.App.ViewModels;

/// <summary>
/// The fixes DAORework applied to the profile's install folder, whether each is still in effect, and the backups to
/// roll back to, with recovery from a change that did not finish (P2.5-06), as <c>fixes status</c>, <c>fixes
/// rollback</c> and <c>fixes recover</c> do.
/// </summary>
public sealed partial class FixStatusViewModel : RecoverableChangesViewModel
{
    private readonly GetProfile _getProfile;
    private readonly GetFixStatus _status;

    public FixStatusViewModel(GetProfile getProfile, GetFixStatus status, RollBackModChange rollBack, RecoverModPlan recover, ILogger<FixStatusViewModel> logger)
        : base(rollBack, recover, logger)
    {
        _getProfile = getProfile;
        _status = status;
    }

    public ObservableCollection<AppliedFixRow> Applied { get; } = [];

    [ObservableProperty]
    public partial string? Summary { get; set; }

    protected override string RollbackConsequence => "the fixes applied before it are the applied ones again.";

    protected override Task<Result<string>> FolderAsync(CancellationToken cancellationToken) => ProfileInstall.ForAsync(_getProfile, ProfileName, cancellationToken);

    /// <summary>What <c>fixes status</c> shows. A profile without an install folder has nothing to show, which the first Apply explains.</summary>
    protected override async Task ReadAsync(CancellationToken cancellationToken)
    {
        Applied.Clear();
        Backups.Clear();
        Summary = null;
        ChangeUnfinished = false;
        if (!(await FolderAsync(cancellationToken)).TryGetValue(out var install, out _))
        {
            return;
        }

        if (!(await _status.ExecuteAsync(install, cancellationToken)).TryGetValue(out var status, out var error))
        {
            ReportError(error);
            return;
        }

        foreach (var fix in status.Applied)
        {
            Applied.Add(AppliedFixRow.From(fix));
        }

        Summary = status.Applied.Count == 0 ? $"No fixes are applied through DAORework in {install}." : $"Install folder: {install}";
        ChangeUnfinished = status.ChangeUnfinished;

        ShowBackups(status.Backups, [.. status.Applied.Select(f => f.LibraryId)], DeploymentChangeText.Fixes, FixName, "no fixes");
    }

    /// <summary>
    /// A fix is recorded by its library id, fixes/&lt;entry&gt;, whose entry starts with the fix's id: it reads as the fix's
    /// name ("4 GB patch", "DXVK"), and an entry of a fix DAORework does not know as the entry.
    /// </summary>
    private static string FixName(string libraryId)
    {
        string[] known = [FixIds.LargeAddressAware, FixIds.Dxvk];
        return known.FirstOrDefault(fix => FixIds.IsEntryOf(libraryId, fix)) is { } fix
            ? FixIds.DisplayName(fix)
            : libraryId[(libraryId.LastIndexOf('/') + 1)..];
    }
}
