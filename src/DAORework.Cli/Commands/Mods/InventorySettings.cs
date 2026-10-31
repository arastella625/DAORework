using System.ComponentModel;
using DAORework.Cli.Commands.Profiles;
using Spectre.Console.Cli;

namespace DAORework.Cli.Commands.Mods;

public sealed class InventorySettings : ProfileNameSettings
{
    [CommandOption("--hash")]
    [Description("Compute a SHA-256 hash for every file (slower).")]
    public bool IncludeHashes { get; init; }
}
