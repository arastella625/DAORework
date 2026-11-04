using DAORework.Core.Common;
using DAORework.Core.Mods.Handover;

namespace DAORework.Application.Mods.Handover;

/// <summary>
/// What copying Vortex's mods did: the copies so far in their order, each mod that could not be copied and why, each
/// mod left out because it runs inside the game, and mods whose order Vortex's record could not settle.
/// </summary>
public sealed record VortexCopyReport(
    IReadOnlyList<VortexCopy> Copies,
    IReadOnlyList<(string Name, Error Error)> Failed,
    IReadOnlyList<string> RunsInTheGame,
    IReadOnlyList<string> Unsettled);
