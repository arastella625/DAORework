using System.Globalization;
using DAORework.Core.Common;
using DAORework.Core.Mods.AddIns;
using Spectre.Console;

namespace DAORework.Cli.Rendering;

/// <summary>Writes the add-ins the game loads, in its order with their Priority, and the files they share (P2.5-23).</summary>
public sealed class AddInsRenderer
{
    private readonly IAnsiConsole _console;

    public AddInsRenderer(IAnsiConsole console)
    {
        _console = console;
    }

    public void Render(AddInsReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        _console.WriteLine("Add-ins:");
        if (report.LoadOrder.Count == 0)
        {
            _console.WriteLine("  AddIns.xml turns on no add-ins.");
            return;
        }

        _console.WriteLine($"  The game loads {report.LoadOrder.Count} in this order. When two have a file of the same name, it uses the copy from the one lower down.");
        _console.WriteLine("  Priority  Add-in");
        foreach (var addIn in report.LoadOrder)
        {
            var official = addIn.IsOfficial ? " (BioWare)" : string.Empty;
            _console.WriteLine($"  {addIn.Priority.ToString(CultureInfo.InvariantCulture),8}  {addIn.Title}{official}");
        }

        _console.WriteLine();
        if (report.Clashes.Count == 0)
        {
            _console.WriteLine("  No two add-ins in the AddIns folder have a file of the same name.");
        }
        else
        {
            _console.WriteLine($"  Files more than one add-in has: {report.Clashes.Count}");
            foreach (var clash in report.Clashes)
            {
                _console.WriteLine($"  - {AddInClashWords.Sentence(clash)}");
            }

            _console.WriteLine($"  {AddInClashWords.Advice}");
        }

        foreach (var archive in report.Unreadable)
        {
            _console.WriteLine($"  Couldn't read {archive}, so what's inside isn't compared.");
        }
    }

    public void RenderProblem(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        _console.WriteLine("Add-ins:");
        _console.WriteLine($"  {error.Message}");
    }
}
