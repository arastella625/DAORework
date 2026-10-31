using System.ComponentModel;
using Spectre.Console.Cli;

namespace DAORework.Cli.Commands.Fixes;

public sealed class FixChangeSettings : FixesProfileSettings
{
    [CommandArgument(0, "<FIX>")]
    [Description("The fix: laa (the 4 GB patch) or dxvk.")]
    public string Fix { get; init; } = string.Empty;

    [CommandArgument(1, "[SOURCE]")]
    [Description("For dxvk: the release as downloaded (.tar.gz), a .zip, an extracted folder, or its x32\\d3d9.dll.")]
    public string? Source { get; init; }

    [CommandOption("--dry-run")]
    [Description("Show the plan and change nothing.")]
    public bool DryRun { get; init; }
}
