using DAORework.Application.Mods.Library;
using DAORework.Cli.Composition;
using DAORework.Cli.Rendering;
using Spectre.Console.Cli;

namespace DAORework.Cli.Commands.Mods;

public sealed class ModsImportCommand : AsyncCommand<ImportSettings>
{
    private readonly ImportMod _importMod;
    private readonly CheckKnownDownload _checkKnownDownload;
    private readonly LibraryRenderer _renderer;
    private readonly ErrorRenderer _errors;

    public ModsImportCommand(ImportMod importMod, CheckKnownDownload checkKnownDownload, LibraryRenderer renderer, ErrorRenderer errors)
    {
        _importMod = importMod;
        _checkKnownDownload = checkKnownDownload;
        _renderer = renderer;
        _errors = errors;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, ImportSettings settings, CancellationToken cancellationToken)
    {
        if (!(await _importMod.ExecuteAsync(new ImportModRequest(settings.Path), cancellationToken)).TryGetValue(out var import, out var error))
        {
            _errors.Render(error, "Import failed");
            return ExitCodes.Failure;
        }

        _renderer.RenderImported(import);
        if (_checkKnownDownload.Execute(import.Entry).TryGetValue(out var check, out var catalogError))
        {
            _renderer.RenderDownloadCheck(import.Entry, check);
        }
        else
        {
            _errors.Render(catalogError, "It could not be compared with the known downloads");
        }

        return ExitCodes.Success;
    }
}
