using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DAORework.Cli.Commands.Mods;

public sealed class RecoverSettings : ModsProfileSettings
{
    [CommandOption("--finish")]
    [Description("Finish the interrupted change.")]
    public bool Finish { get; init; }

    [CommandOption("--rollback")]
    [Description("Undo the interrupted change from its backup.")]
    public bool RollBack { get; init; }

    public override ValidationResult Validate() =>
        Finish == RollBack ? ValidationResult.Error("Choose exactly one of --finish or --rollback.") : ValidationResult.Success();
}
