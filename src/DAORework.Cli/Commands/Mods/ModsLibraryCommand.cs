using DAORework.Application.Mods.Library;
using DAORework.Cli.Rendering;
using Spectre.Console.Cli;

namespace DAORework.Cli.Commands.Mods;

public sealed class ModsLibraryCommand : AsyncCommand
{
    private readonly ListLibrary _listLibrary;
    private readonly LibraryRenderer _renderer;

    public ModsLibraryCommand(ListLibrary listLibrary, LibraryRenderer renderer)
    {
        _listLibrary = listLibrary;
        _renderer = renderer;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, CancellationToken cancellationToken)
    {
        _renderer.RenderList((await _listLibrary.ExecuteAsync(cancellationToken)).Value);
        return Composition.ExitCodes.Success;
    }
}
