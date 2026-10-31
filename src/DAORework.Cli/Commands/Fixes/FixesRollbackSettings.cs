using System.ComponentModel;
using Spectre.Console.Cli;

namespace DAORework.Cli.Commands.Fixes;

public sealed class FixesRollbackSettings : FixesProfileSettings
{
    [CommandOption("--backup <ID>")]
    [Description("The backup to go back to, as fixes status lists them (default: the most recent change).")]
    public string? BackupId { get; init; }
}
