using System.ComponentModel;
using Spectre.Console.Cli;

namespace DAORework.Cli.Commands.Mods;

/// <summary>Where <c>mods downloads</c> looks.</summary>
public sealed class DownloadsSettings : CommandSettings
{
    [CommandOption("--folder <PATH>")]
    [Description("The folder to look in (default: the one set in the window, or your Downloads folder).")]
    public string? Folder { get; init; }
}
