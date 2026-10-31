using System.ComponentModel;
using Spectre.Console.Cli;

namespace DAORework.Cli.Commands.Fixes;

/// <summary>The profile whose install folder a fix command works on.</summary>
public class FixesProfileSettings : CommandSettings
{
    [CommandOption("-p|--profile <PROFILE>")]
    [Description("The profile whose install folder to change (default: Default).")]
    public string Profile { get; init; } = "Default";

    [CommandOption("-y|--yes")]
    [Description("Apply without asking. Needed when the console cannot ask, as in scripts.")]
    public bool Yes { get; init; }
}
