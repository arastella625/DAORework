using System.ComponentModel;
using Spectre.Console.Cli;

namespace DAORework.Cli.Commands.Profiles;

/// <summary>Unsealed on purpose: settings that add options derive from it.</summary>
public class ProfileNameSettings : CommandSettings
{
    [CommandArgument(0, "[profile]")]
    [Description("Profile name. Defaults to \"Default\".")]
    public string Profile { get; init; } = "Default";
}
