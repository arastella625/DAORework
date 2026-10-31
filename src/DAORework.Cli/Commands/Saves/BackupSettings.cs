using System.ComponentModel;
using DAORework.Cli.Commands.Profiles;
using DAORework.Core.Saves;
using Spectre.Console.Cli;

namespace DAORework.Cli.Commands.Saves;

public sealed class BackupSettings : ProfileNameSettings
{
    [CommandOption("-k|--keep <COUNT>")]
    [Description("How many backups to keep for this profile, the new one included (default 10). Older ones are deleted.")]
    public int KeepLatest { get; init; } = SaveBackupRetention.DefaultKeepLatest;
}
