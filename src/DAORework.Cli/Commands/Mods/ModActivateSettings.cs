using System.ComponentModel;
using Spectre.Console.Cli;

namespace DAORework.Cli.Commands.Mods;

/// <summary><c>mods activate</c>'s settings: a mod change, and the mods that run inside the game the player says yes to (ADR-0033 §3).</summary>
public sealed class ModActivateSettings : ModChangeSettings
{
    [CommandOption("--allow-programs <MOD_ID>")]
    [Description("Say you trust a mod that runs its own program inside the game, naming it. --yes doesn't cover these.")]
    public string[] AllowPrograms { get; init; } = [];
}
