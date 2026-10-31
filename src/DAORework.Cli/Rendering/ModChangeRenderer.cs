using System.Globalization;
using DAORework.Application.Mods.Changes;
using DAORework.Core.Mods.Apply;
using DAORework.Core.Mods.Backups;
using DAORework.Core.Mods.Planning;
using Spectre.Console;

namespace DAORework.Cli.Rendering;

/// <summary>The dry run, the confirmation, and what a mod change did (ROADMAP P2-06). Plain text, so it reads in scripts too.</summary>
public sealed class ModChangeRenderer
{
    private readonly IAnsiConsole _console;

    public ModChangeRenderer(IAnsiConsole console)
    {
        _console = console;
    }

    public void RenderPlan(ModActivationPlan plan, bool listFiles)
    {
        foreach (var line in ModActivationPlanText.Render(plan, listFiles))
        {
            _console.WriteLine(line);
        }
    }

    /// <summary>Asks before a change. With --yes it does not ask; when the console cannot ask, it refuses rather than assume.</summary>
    public bool Confirm(bool yes, string question)
    {
        if (yes)
        {
            return true;
        }

        if (!_console.Profile.Capabilities.Interactive)
        {
            _console.WriteLine("Nothing was changed: add --yes to apply without a prompt.");
            return false;
        }

        var confirmed = _console.Confirm(question, defaultValue: false);
        if (!confirmed)
        {
            _console.WriteLine("Nothing was changed.");
        }

        return confirmed;
    }

    public void Say(string message) => _console.WriteLine(message);

    public void RenderApplied(ApplyReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        _console.WriteLine($"Done: {report.StepsRun} step(s) applied{(report.StepsAlreadyDone > 0 ? $", {report.StepsAlreadyDone} already done" : string.Empty)}.");
        _console.WriteLine($"Backup {report.BackupId} holds what was there before. Undo with: mods rollback");
    }

    public void RenderRestored(RestoreReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        _console.WriteLine($"Rolled back: {report.Restored} file(s) put back, {report.Removed} removed.");
        foreach (var path in report.LeftInPlace)
        {
            _console.WriteLine($"Left in place (changed since): {path}");
        }
    }

    public void RenderStatus(string documents, ModStatus status)
    {
        ArgumentNullException.ThrowIfNull(status);
        _console.WriteLine($"Documents folder: {documents}");
        _console.WriteLine(status.Record.ActiveModIds.Count == 0
            ? "No mods are active through DAORework."
            : $"Active, lowest priority first: {string.Join(", ", status.Record.ActiveModIds)} ({status.Record.Files.Count} file(s), {status.Record.AddedAddIns.Count} add-in entr{(status.Record.AddedAddIns.Count == 1 ? "y" : "ies")})");
        if (status.ChangeUnfinished)
        {
            _console.WriteLine("A change did not finish. Run: mods recover --finish, or mods recover --rollback");
        }

        foreach (var backup in status.Backups.Where(b => b.PreviousRecord is not null).Take(5))
        {
            var when = backup.CreatedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            _console.WriteLine($"Backup {backup.Id} ({when}): before it, {(backup.PreviousRecord!.ActiveModIds.Count == 0 ? "no mods" : string.Join(", ", backup.PreviousRecord.ActiveModIds))}");
        }
    }
}
