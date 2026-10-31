using System.ComponentModel;
using Spectre.Console.Cli;

namespace DAORework.Cli.Commands.Mods;

public sealed class ModChangeSettings : ModsProfileSettings
{
    [CommandArgument(0, "<MOD_ID>")]
    [Description("One or more library ids, as mods library shows them.")]
    public string[] ModIds { get; init; } = [];

    [CommandOption("--dry-run")]
    [Description("Show the plan and change nothing.")]
    public bool DryRun { get; init; }

    [CommandOption("--files")]
    [Description("List every file the plan touches, not just the counts.")]
    public bool ListFiles { get; init; }
}
