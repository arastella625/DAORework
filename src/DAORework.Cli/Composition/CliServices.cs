using DAORework.Cli.Commands.Mods;
using DAORework.Cli.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace DAORework.Cli.Composition;

public static class CliServices
{
    /// <summary>Renderers are the only classes that write to the console (SDD §9.1).</summary>
    public static IServiceCollection AddCliRendering(this IServiceCollection services)
    {
        services.AddSingleton<ErrorRenderer>();
        services.AddSingleton<LibraryRenderer>();
        services.AddSingleton<ModChangeRenderer>();
        services.AddSingleton<AdoptionRenderer>();

        // Not a renderer: the second half of mods adopt, kept apart so the command stays within five dependencies.
        services.AddSingleton<AdoptionApplier>();
        services.AddSingleton<FixRenderer>();
        services.AddSingleton<ProfileRenderer>();
        services.AddSingleton<InstallationRenderer>();
        services.AddSingleton<DocumentsFolderRenderer>();
        services.AddSingleton<DiagnosticReportRenderer>();
        services.AddSingleton<ModInspectionRenderer>();
        services.AddSingleton<ModInventoryRenderer>();
        services.AddSingleton<SaveBackupRenderer>();
        return services;
    }
}
