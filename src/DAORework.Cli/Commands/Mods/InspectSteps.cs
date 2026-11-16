using DAORework.Application.Mods;
using DAORework.Application.Mods.AddIns;

namespace DAORework.Cli.Commands.Mods;

/// <summary>What <c>mods inspect</c> reads, grouped so the command stays within five dependencies.</summary>
public sealed record InspectSteps(InspectModFolders Folders, FindAddInClashes AddIns);
