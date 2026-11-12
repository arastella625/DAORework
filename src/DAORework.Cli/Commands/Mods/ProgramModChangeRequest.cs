using DAORework.Core.Mods.Planning;

namespace DAORework.Cli.Commands.Mods;

/// <param name="Kind">Turning on or off.</param>
/// <param name="Install">The profile's install folder, where such a mod goes; null when it has none.</param>
/// <param name="Settings">What the command was given.</param>
/// <param name="Allowed">The mods the player said yes to with <c>--allow-programs</c>.</param>
public sealed record ProgramModChangeRequest(ModChangeKind Kind, string? Install, ModChangeSettings Settings, IReadOnlyCollection<string> Allowed);
