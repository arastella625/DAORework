using CommunityToolkit.Mvvm.ComponentModel;
using DAORework.Application.Mods.Apply;
using DAORework.Application.Mods.Changes;
using DAORework.Application.Profiles;
using DAORework.Core.Common;
using DAORework.Core.Mods.Backups;
using Microsoft.Extensions.Logging;

namespace DAORework.App.ViewModels;

/// <summary>
/// The Mods tab's <i>Changes</i> (P3-04), as <c>mods status</c>, <c>mods rollback</c> and <c>mods recover</c>: what is
/// active, the backups to roll back to, each asked for once more before anything is put back, and the way out of a
/// change that did not finish.
/// </summary>
public sealed partial class ModChangesViewModel : RecoverableChangesViewModel
{
    private readonly GetProfile _getProfile;
    private readonly GetModStatus _status;

    public ModChangesViewModel(GetProfile getProfile, GetModStatus status, RollBackModChange rollBack, RecoverModPlan recover, ILogger<ModChangesViewModel> logger)
        : base(rollBack, recover, logger)
    {
        _getProfile = getProfile;
        _status = status;
    }

    [ObservableProperty]
    public partial string? Summary { get; set; }

    /// <summary>Gets or sets how a mod id is shown: by its Library name in the window (P2.5-19), as it is otherwise.</summary>
    public Func<string, string> NameOf { get; set; } = id => id;

    protected override string RollbackConsequence => "the mods active before it are the active ones again.";

    protected override Task<Result<string>> FolderAsync(CancellationToken cancellationToken) => ProfileDocuments.ForAsync(_getProfile, ProfileName, cancellationToken);

    /// <summary>What <c>mods status</c> shows.</summary>
    protected override async Task ReadAsync(CancellationToken cancellationToken)
    {
        Backups.Clear();
        Summary = null;
        ChangeUnfinished = false;
        if (!(await FolderAsync(cancellationToken)).TryGetValue(out var documents, out _))
        {
            return;
        }

        if (!(await _status.ExecuteAsync(documents, cancellationToken)).TryGetValue(out var status, out var error))
        {
            ReportError(error);
            return;
        }

        var active = status.Record.ActiveModIds;
        Summary = active.Count == 0
            ? $"No mods are active through DAORework in {documents}."
            : $"Active in {documents}, lowest priority first: {string.Join(", ", active.Select(NameOf))} ({status.Record.Files.Count} file(s)).";
        ChangeUnfinished = status.ChangeUnfinished;
        ShowBackups(status.Backups, active, DeploymentChangeText.Mods, NameOf, "no mods");
    }
}
