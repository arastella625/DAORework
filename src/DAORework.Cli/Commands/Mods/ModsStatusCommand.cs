using DAORework.Application.Mods.Changes;
using DAORework.Application.Profiles;
using DAORework.Cli.Composition;
using DAORework.Cli.Rendering;
using Spectre.Console.Cli;

namespace DAORework.Cli.Commands.Mods;

public sealed class ModsStatusCommand : AsyncCommand<ModsProfileSettings>
{
    private readonly GetProfile _getProfile;
    private readonly GetModStatus _status;
    private readonly ModChangeRenderer _renderer;
    private readonly ErrorRenderer _errors;

    public ModsStatusCommand(GetProfile getProfile, GetModStatus status, ModChangeRenderer renderer, ErrorRenderer errors)
    {
        _getProfile = getProfile;
        _status = status;
        _renderer = renderer;
        _errors = errors;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, ModsProfileSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (!(await ModsDocuments.ForProfileAsync(_getProfile, settings.Profile, cancellationToken)).TryGetValue(out var documents, out var error)
            || !(await _status.ExecuteAsync(documents, cancellationToken)).TryGetValue(out var status, out error))
        {
            _errors.Render(error);
            return ExitCodes.Failure;
        }

        _renderer.RenderStatus(documents, status);
        return ExitCodes.Success;
    }
}
