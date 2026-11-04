using DAORework.Application.Mods.Handover;
using DAORework.Core.Mods.Handover;
using Spectre.Console;

namespace DAORework.Cli.Rendering;

/// <summary>Writes where the handover from Vortex stands (ADR-0028 §5), and what <c>mods vortex copy</c> did.</summary>
public sealed class VortexRenderer
{
    private readonly IAnsiConsole _console;

    public VortexRenderer(IAnsiConsole console)
    {
        _console = console;
    }

    public void RenderState(VortexHandoverState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        foreach (var mod in state.Managed)
        {
            _console.WriteLine($"  {mod.Name}  ({mod.FilesInGame} file(s) in the game)");
        }

        _console.WriteLine(state.Step switch
        {
            VortexHandoverStep.NothingToDo => "Vortex manages no mods in this documents folder.",
            VortexHandoverStep.Copy => "Next: mods vortex copy, which adds a copy of each to the library. Nothing in the game changes.",
            VortexHandoverStep.Purge => "Next: open Vortex, choose Dragon Age: Origins, Purge on the Mods page, then stop managing the game. Run this again to check.",
            VortexHandoverStep.TurnOn => "Next, in this order: mods activate " + string.Join(' ', state.Copies.Select(c => c.ModId)),
            _ => "Done: every copy is on.",
        });
        if (state.PlacingAgain)
        {
            _console.WriteLine("Vortex is placing mods again. Stop managing Dragon Age in Vortex, or it will keep changing your game.");
        }

        if (state.LostDlc.Count > 0)
        {
            _console.WriteLine($"The purge took {string.Join(", ", state.LostDlc.Select(c => c.ModId))} out of AddIns.xml. Press Turn them on on the window's Move from Vortex card to put it back.");
        }
    }

    public void RenderCopied(VortexCopyReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        _console.WriteLine($"{report.Copies.Count} mod(s) copied into the library. Nothing in the game changed.");
        foreach (var (name, error) in report.Failed)
        {
            _console.WriteLine($"  {name} was not copied: {error.Message} ({error.Code})");
        }

        foreach (var name in report.RunsInTheGame)
        {
            _console.WriteLine($"  {name} runs inside the game, so it is left out of the handover.");
        }

        if (report.Unsettled.Count > 0)
        {
            _console.WriteLine($"  Vortex's record does not settle the order of {string.Join(", ", report.Unsettled)}: check which copy you want after turning them on.");
        }
    }
}
