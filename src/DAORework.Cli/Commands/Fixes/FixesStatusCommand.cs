using DAORework.Application.Fixes;
using DAORework.Application.Profiles;
using DAORework.Cli.Composition;
using DAORework.Cli.Rendering;
using Spectre.Console.Cli;

namespace DAORework.Cli.Commands.Fixes;

public sealed class FixesStatusCommand : AsyncCommand<FixesProfileSettings>
{
    private readonly GetProfile _getProfile;
    private readonly GetFixStatus _status;
    private readonly FixRenderer _renderer;
    private readonly ErrorRenderer _errors;

    public FixesStatusCommand(GetProfile getProfile, GetFixStatus status, FixRenderer renderer, ErrorRenderer errors)
    {
        _getProfile = getProfile;
        _status = status;
        _renderer = renderer;
        _errors = errors;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, FixesProfileSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (!(await FixesInstall.ForProfileAsync(_getProfile, settings.Profile, cancellationToken)).TryGetValue(out var install, out var error)
            || !(await _status.ExecuteAsync(install, cancellationToken)).TryGetValue(out var status, out error))
        {
            _errors.Render(error);
            return ExitCodes.Failure;
        }

        _renderer.RenderStatus(install, status);
        return ExitCodes.Success;
    }
}
