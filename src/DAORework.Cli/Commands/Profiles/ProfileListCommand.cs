using DAORework.Application.Profiles;
using DAORework.Cli.Composition;
using DAORework.Cli.Rendering;
using Spectre.Console.Cli;

namespace DAORework.Cli.Commands.Profiles;

public sealed class ProfileListCommand : AsyncCommand
{
    private readonly ListProfiles _listProfiles;
    private readonly ProfileRenderer _renderer;

    public ProfileListCommand(ListProfiles listProfiles, ProfileRenderer renderer)
    {
        _listProfiles = listProfiles;
        _renderer = renderer;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, CancellationToken cancellationToken)
    {
        var names = (await _listProfiles.ExecuteAsync(new ListProfilesRequest(), cancellationToken)).Value;
        _renderer.RenderNames(names);
        return ExitCodes.Success;
    }
}
