using DAORework.Application.Mods.Handover;
using DAORework.Application.Profiles;
using DAORework.Cli.Composition;
using DAORework.Cli.Rendering;
using Spectre.Console.Cli;

namespace DAORework.Cli.Commands.Mods;

/// <summary><c>mods vortex copy</c> (ADR-0028 §5): step 1 of the handover. Writes only DAORework's own folder.</summary>
public sealed class ModsVortexCopyCommand : AsyncCommand<ModsProfileSettings>
{
    private readonly GetProfile _getProfile;
    private readonly CopyVortexMods _copy;
    private readonly VortexRenderer _renderer;
    private readonly ErrorRenderer _errors;

    public ModsVortexCopyCommand(GetProfile getProfile, CopyVortexMods copy, VortexRenderer renderer, ErrorRenderer errors)
    {
        _getProfile = getProfile;
        _copy = copy;
        _renderer = renderer;
        _errors = errors;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, ModsProfileSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (!(await ModsDocuments.ForProfileAsync(_getProfile, settings.Profile, cancellationToken)).TryGetValue(out var documents, out var error)
            || !(await _copy.ExecuteAsync(documents, cancellationToken)).TryGetValue(out var report, out error))
        {
            _errors.Render(error);
            return ExitCodes.Failure;
        }

        _renderer.RenderCopied(report);
        return report.Failed.Count == 0 ? ExitCodes.Success : ExitCodes.Failure;
    }
}
