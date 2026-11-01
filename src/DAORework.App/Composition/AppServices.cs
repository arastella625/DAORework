using DAORework.App.Services;
using DAORework.App.Themes;
using DAORework.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace DAORework.App.Composition;

/// <summary>Registers the presentation layer: one view model per page, the picker that opens the OS dialogs, and the theme.</summary>
public static class AppServices
{
    public static IServiceCollection AddAppViewModels(this IServiceCollection services)
    {
        services.AddSingleton<IPathPicker, StorageProviderPathPicker>();

        // The switcher belongs to the running App, which creates it before any window (ADR-0016).
        services.AddSingleton(_ => ((App)Avalonia.Application.Current!).Themes);
        services.AddSingleton<LookPresenter>();
        services.AddSingleton<ThemePreference>();
        services.AddTransient<LookPictureEditor>();
        services.AddTransient<ProfileListViewModel>();
        services.AddTransient<ProfilePathsViewModel>();
        services.AddTransient<InstallsViewModel>();
        services.AddTransient<ProfileLookViewModel>();
        services.AddTransient<ProfilePageViewModel>();
        services.AddTransient<DiagnosticsPageViewModel>();
        services.AddTransient<ModsPageViewModel>();
        services.AddTransient<ModAdoptionViewModel>();
        services.AddTransient<HandInstalledViewModel>();
        services.AddTransient<LibraryViewModel>();
        services.AddTransient<ModActivationViewModel>();
        services.AddTransient<ModChangesViewModel>();
        services.AddTransient<LoadOrderViewModel>();
        services.AddTransient<DeployedModsViewModel>();
        services.AddTransient<ModsTabViewModel>();
        services.AddTransient<SavesPageViewModel>();
        services.AddTransient<FixStatusViewModel>();
        services.AddTransient<FixChangeViewModel>();
        services.AddTransient<FixesTabViewModel>();
        services.AddTransient<LookListViewModel>();
        services.AddTransient<LookDetailsViewModel>();
        services.AddTransient<LookPictureViewModel>();
        services.AddTransient<SettingsPageViewModel>();
        services.AddTransient<NextStepsViewModel>();
        services.AddTransient<HomeViewModel>();
        services.AddTransient<PagesViewModel>();
        services.AddTransient<LaunchViewModel>();
        services.AddTransient<MainWindowViewModel>();
        return services;
    }
}
