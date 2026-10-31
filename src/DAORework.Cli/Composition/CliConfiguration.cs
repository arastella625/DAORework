using DAORework.Cli.Commands;
using DAORework.Cli.Commands.Fixes;
using DAORework.Cli.Commands.Mods;
using DAORework.Cli.Commands.Profiles;
using DAORework.Cli.Commands.Saves;
using Serilog;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DAORework.Cli.Composition;

/// <summary>One command class per verb (ADR-0005). Shared by Program.cs and the CLI tests.</summary>
public static class CliConfiguration
{
    public static void Configure(IConfigurator config)
    {
        ArgumentNullException.ThrowIfNull(config);

        config.SetApplicationName("dao");

        config.AddCommand<DetectCommand>("detect").WithDescription("Detect Dragon Age: Origins installations and documents folders");
        config.AddCommand<InitCommand>("init").WithDescription("Create a runtime profile, pre-filled from the best detected install");
        config.AddCommand<ProfileListCommand>("profiles").WithDescription("List saved runtime profiles");
        config.AddBranch("profile", profile =>
        {
            profile.SetDescription("Show or edit a runtime profile");
            profile.AddCommand<ProfileListCommand>("list").WithDescription("List saved runtime profiles");
            profile.AddCommand<ProfileShowCommand>("show").WithDescription("Show a saved runtime profile");
            profile.AddCommand<ProfileSetTargetCommand>("set-target").WithDescription("Set the launch target (Game or OfficialLauncher)");
            profile.AddCommand<ProfileSetPathCommand>("set-path").WithDescription("Set a profile path (install, game, launcher or documents)");
        });
        config.AddCommand<DiagnoseCommand>("diagnose").WithDescription("Validate a saved runtime profile and write a diagnostic report");
        config.AddCommand<LaunchCommand>("launch").WithDescription("Launch Dragon Age: Origins using a saved profile");
        config.AddCommand<SupportBundleCommand>("support-bundle").WithDescription("Write one zip with the profile, diagnostics, mod inventory and logs, for a bug report");
        config.AddBranch("mods", mods =>
        {
            mods.SetDescription("Inspect the mod folders, import mods into the library, and activate them");
            mods.AddCommand<ModsInspectCommand>("inspect").WithDescription("Summarize the override and AddIns folders");
            mods.AddCommand<ModsInventoryCommand>("inventory").WithDescription("Write a JSON inventory of every mod file");
            mods.AddCommand<ModsImportCommand>("import").WithDescription("Add a .dazip, .zip or mod folder to DAORework's library (installs nothing)");
            mods.AddCommand<ModsLibraryCommand>("library").WithDescription("List the mods in DAORework's library");
            mods.AddCommand<ModsStatusCommand>("status").WithDescription("Show the mods DAORework has activated in the profile's documents folder, and its backups");
            mods.AddCommand<ModsActivateCommand>("activate").WithDescription("Install library mods into the documents folder: shows the plan, asks, backs up, applies");
            mods.AddCommand<ModsDeactivateCommand>("deactivate").WithDescription("Remove mods DAORework installed: shows the plan, asks, backs up, applies");
            mods.AddCommand<ModsRollbackCommand>("rollback").WithDescription("Undo the most recent mod change (or --backup <id>) from its backup");
            mods.AddCommand<ModsAdoptCommand>("adopt").WithDescription("Take over mods installed by hand: lists them, or with ids shows the plan, asks, backs up, applies");
            mods.AddCommand<ModsRecoverCommand>("recover").WithDescription("Settle a mod change that was interrupted: --finish or --rollback");
        });

        config.AddBranch("fixes", fixes =>
        {
            fixes.SetDescription("Apply managed fixes to the game install folder, with a backup: the 4 GB patch and DXVK");
            fixes.AddCommand<FixesStatusCommand>("status").WithDescription("Show the fixes DAORework has applied to the profile's install folder, and its backups");
            fixes.AddCommand<FixesApplyCommand>("apply").WithDescription("Apply a fix (laa, or dxvk <release>): shows the plan, asks, backs up, applies");
            fixes.AddCommand<FixesRemoveCommand>("remove").WithDescription("Remove a fix DAORework applied, putting back the original files");
            fixes.AddCommand<FixesRollbackCommand>("rollback").WithDescription("Undo the most recent fix change (or --backup <id>) from its backup");
            fixes.AddCommand<FixesRecoverCommand>("recover").WithDescription("Settle a fix change that was interrupted: --finish or --rollback");
        });
        config.AddBranch("saves", saves =>
        {
            saves.SetDescription("Back up saved games (copied out of the saves folder, never changed)");
            saves.AddCommand<SavesBackupCommand>("backup").WithDescription("Copy the profile's saves into a new backup and keep the newest ones");
            saves.AddCommand<SavesListCommand>("list").WithDescription("List the profile's save backups, newest first");
        });

        // Parse errors happen before the type resolver exists, so fall back to the console the app was configured with.
        config.SetExceptionHandler((exception, resolver) => HandleException(exception, resolver, config.Settings.Console));
    }

    private static int HandleException(Exception exception, ITypeResolver? resolver, IAnsiConsole? configuredConsole)
    {
        var console = resolver?.Resolve(typeof(IAnsiConsole)) as IAnsiConsole ?? configuredConsole ?? AnsiConsole.Console;

        // Parse and validation errors: the plain message, never Spectre's pretty renderable,
        // which produces no output on a redirected console.
        if (exception is CommandAppException usage)
        {
            console.WriteLine(usage.Message);
            return ExitCodes.InvalidUsage;
        }

        Log.Fatal(exception, "DAORework failed unexpectedly.");
        console.WriteLine($"DAORework failed unexpectedly: {exception.Message}");
        return ExitCodes.Unexpected;
    }
}
