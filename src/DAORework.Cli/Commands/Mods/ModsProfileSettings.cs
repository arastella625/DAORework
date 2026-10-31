using System.ComponentModel;
using Spectre.Console.Cli;

namespace DAORework.Cli.Commands.Mods;

/// <summary>The profile whose documents folder a mod change works on.</summary>
public class ModsProfileSettings : CommandSettings
{
    [CommandOption("-p|--profile <PROFILE>")]
    [Description("The profile whose documents folder to change (default: Default).")]
    public string Profile { get; init; } = "Default";

    [CommandOption("-y|--yes")]
    [Description("Apply without asking. Needed when the console cannot ask, as in scripts.")]
    public bool Yes { get; init; }
}
