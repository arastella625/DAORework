using System.Globalization;
using DAORework.Application.Fixes;
using DAORework.Core.Fixes;
using DAORework.Core.Mods.Backups;
using Spectre.Console;

namespace DAORework.Cli.Rendering;

/// <summary>A fix plan, what applying it did, and which fixes are in effect (ADR-0013). Asking and rollback output are shared with mod changes.</summary>
public sealed class FixRenderer
{
    private readonly IAnsiConsole _console;
    private readonly ModChangeRenderer _changes;

    public FixRenderer(IAnsiConsole console, ModChangeRenderer changes)
    {
        _console = console;
        _changes = changes;
    }

    public void RenderPlan(PlannedFix planned)
    {
        ArgumentNullException.ThrowIfNull(planned);
        foreach (var line in FixPlanText.Render(planned.Plan, planned.FixName))
        {
            _console.WriteLine(line);
        }

        foreach (var note in planned.Notes)
        {
            _console.WriteLine($"Note: {note}");
        }
    }

    public bool Confirm(bool yes, string question) => _changes.Confirm(yes, question);

    public void Say(string message) => _console.WriteLine(message);

    public void RenderRestored(RestoreReport report) => _changes.RenderRestored(report);

    public void RenderApplied(FixApplyOutcome outcome)
    {
        ArgumentNullException.ThrowIfNull(outcome);
        if (outcome.Report is not { } report)
        {
            _console.WriteLine("Nothing on disk needed changing; DAORework's record of the install folder is up to date.");
            return;
        }

        _console.WriteLine($"Done: {report.StepsRun} step(s) applied{(report.StepsAlreadyDone > 0 ? $", {report.StepsAlreadyDone} already done" : string.Empty)}.");
        _console.WriteLine($"Backup {report.BackupId} holds what was there before. Undo with: fixes rollback");
    }

    public void RenderStatus(string install, FixStatus status)
    {
        ArgumentNullException.ThrowIfNull(status);
        _console.WriteLine($"Install folder: {install}");
        if (status.Applied.Count == 0)
        {
            _console.WriteLine("No fixes are applied through DAORework.");
        }

        foreach (var fix in status.Applied)
        {
            var name = $"{fix.FixName}{(fix.Version is null ? string.Empty : $" {fix.Version}")} ({fix.LibraryId[(fix.LibraryId.LastIndexOf('/') + 1)..]})";
            _console.WriteLine(fix.InEffect
                ? $"{name}: in effect"
                : $"{name}: no longer in effect; changed outside DAORework since: {string.Join(", ", fix.NotInEffect)}");
        }

        if (status.ChangeUnfinished)
        {
            _console.WriteLine("A change did not finish. Run: fixes recover --finish, or fixes recover --rollback");
        }

        foreach (var backup in status.Backups.Where(b => b.PreviousRecord is not null).Take(5))
        {
            var when = backup.CreatedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            var before = backup.PreviousRecord!.ActiveModIds.Select(id => id[(id.LastIndexOf('/') + 1)..]).ToList();
            _console.WriteLine($"Backup {backup.Id} ({when}): before it, {(before.Count == 0 ? "no fixes" : string.Join(", ", before))}");
        }
    }
}
