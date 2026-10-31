using System.ComponentModel;
using Spectre.Console.Cli;

namespace DAORework.Cli.Commands.Mods;

public sealed class ImportSettings : CommandSettings
{
    [CommandArgument(0, "<PATH>")]
    [Description("A .dazip, a .zip (a Nexus download with a .dazip inside works too) or an extracted mod folder.")]
    public string Path { get; init; } = string.Empty;
}
