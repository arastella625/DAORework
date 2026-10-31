using System.ComponentModel;
using Spectre.Console.Cli;

namespace DAORework.Cli.Commands.Mods;

public sealed class RollbackSettings : ModsProfileSettings
{
    [CommandOption("--backup <ID>")]
    [Description("The backup to go back to, as mods status lists them (default: the most recent change).")]
    public string? BackupId { get; init; }
}
