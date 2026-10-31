using System.ComponentModel;
using Spectre.Console.Cli;

namespace DAORework.Cli.Commands;

public sealed class DetectSettings : CommandSettings
{
    [CommandOption("-p|--path <DIRECTORY>")]
    [Description("A folder to check as an install, in addition to the automatic sources. Repeatable.")]
    public string[] Paths { get; init; } = [];
}
