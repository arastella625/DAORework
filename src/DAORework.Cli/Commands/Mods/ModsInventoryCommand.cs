using DAORework.Application.Abstractions;
using DAORework.Application.Mods;
using DAORework.Application.Profiles;
using DAORework.Cli.Composition;
using DAORework.Cli.Rendering;
using Spectre.Console.Cli;

namespace DAORework.Cli.Commands.Mods;

public sealed class ModsInventoryCommand : AsyncCommand<InventorySettings>
{
    private readonly GetProfile _getProfile;
    private readonly BuildModInventory _buildModInventory;
    private readonly IModInventoryWriter _writer;
    private readonly ModInventoryRenderer _renderer;
    private readonly ErrorRenderer _errors;

    public ModsInventoryCommand(
        GetProfile getProfile,
        BuildModInventory buildModInventory,
        IModInventoryWriter writer,
        ModInventoryRenderer renderer,
        ErrorRenderer errors)
    {
        _getProfile = getProfile;
        _buildModInventory = buildModInventory;
        _writer = writer;
        _renderer = renderer;
        _errors = errors;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, InventorySettings settings, CancellationToken cancellationToken)
    {
        if (!(await _getProfile.ExecuteAsync(new GetProfileRequest(settings.Profile), cancellationToken)).TryGetValue(out var profile, out var error))
        {
            _errors.Render(error);
            return ExitCodes.Failure;
        }

        var request = new BuildModInventoryRequest(profile, settings.IncludeHashes);
        if (!(await _buildModInventory.ExecuteAsync(request, cancellationToken)).TryGetValue(out var inventory, out var inventoryError))
        {
            _errors.Render(inventoryError, "Inventory failed");
            return ExitCodes.Failure;
        }

        var reportPath = await _writer.WriteAsync(inventory, cancellationToken);
        _renderer.Render(inventory, reportPath);
        return ExitCodes.Success;
    }
}
