using DAORework.Application.Mods.Handover;
using DAORework.Application.Profiles;
using DAORework.Cli.Composition;
using DAORework.Cli.Rendering;
using Spectre.Console.Cli;

namespace DAORework.Cli.Commands.Mods;

/// <summary><c>mods vortex</c> (ADR-0028 §5): the mods Vortex still manages and the handover's current step. Only reads.</summary>
public sealed class ModsVortexCommand : AsyncCommand<ModsProfileSettings>
{
    private readonly GetProfile _getProfile;
    private readonly ReadVortexHandover _read;
    private readonly VortexRenderer _renderer;
    private readonly ErrorRenderer _errors;

    public ModsVortexCommand(GetProfile getProfile, ReadVortexHandover read, VortexRenderer renderer, ErrorRenderer errors)
    {
        _getProfile = getProfile;
        _read = read;
        _renderer = renderer;
        _errors = errors;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, ModsProfileSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (!(await ModsDocuments.ForProfileAsync(_getProfile, settings.Profile, cancellationToken)).TryGetValue(out var documents, out var error)
            || !(await _read.ExecuteAsync(documents, cancellationToken)).TryGetValue(out var state, out error))
        {
            _errors.Render(error);
            return ExitCodes.Failure;
        }

        _renderer.RenderState(state);
        return ExitCodes.Success;
    }
}
