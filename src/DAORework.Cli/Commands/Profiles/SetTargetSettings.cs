using System.ComponentModel;
using DAORework.Core.Launching;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DAORework.Cli.Commands.Profiles;

public sealed class SetTargetSettings : CommandSettings
{
    [CommandArgument(0, "<profile>")]
    [Description("Profile name.")]
    public string Profile { get; init; } = string.Empty;

    [CommandArgument(1, "<target>")]
    [Description("Game or OfficialLauncher.")]
    public string Target { get; init; } = string.Empty;

    public LaunchTarget ParsedTarget => Enum.Parse<LaunchTarget>(Target, ignoreCase: true);

    public override ValidationResult Validate() =>
        Enum.TryParse<LaunchTarget>(Target, ignoreCase: true, out _)
            ? ValidationResult.Success()
            : ValidationResult.Error($"Invalid launch target: {Target}. Valid values: Game, OfficialLauncher");
}
