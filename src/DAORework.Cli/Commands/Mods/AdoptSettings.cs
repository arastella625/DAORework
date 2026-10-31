using System.ComponentModel;
using Spectre.Console.Cli;

namespace DAORework.Cli.Commands.Mods;

/// <summary>
/// <c>mods adopt</c> (ADR-0015): without candidate ids it lists what could be adopted; with them it shows the plan,
/// asks, backs up and applies, like <c>mods activate</c>.
/// </summary>
public sealed class AdoptSettings : ModsProfileSettings
{
    [CommandArgument(0, "[CANDIDATE]")]
    [Description("Candidate ids to adopt, in order (later ones win by file name), as mods adopt --dry-run lists them.")]
    public string[] Candidates { get; init; } = [];

    [CommandOption("--dry-run")]
    [Description("List what could be adopted, or with ids show the plan, and change nothing.")]
    public bool DryRun { get; init; }

    [CommandOption("--files")]
    [Description("List every file, not just the counts.")]
    public bool ListFiles { get; init; }

    [CommandOption("--name <NAME>")]
    [Description("The name for the loose override files (override:(loose)) as one mod.")]
    public string? Name { get; init; }
}
