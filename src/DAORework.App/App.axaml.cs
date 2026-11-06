using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DAORework.App.Composition;
using DAORework.App.Services;
using DAORework.App.Themes;
using DAORework.App.ViewModels;
using DAORework.App.Views;
using DAORework.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace DAORework.App;

/// <summary>The Avalonia application. On the desktop it builds the service provider and opens the main window.</summary>
public sealed partial class App : Avalonia.Application
{
    private ThemeSwitcher? _themes;

    /// <summary>Gets the theme switcher, which <see cref="Initialize"/> creates before any window opens.</summary>
    public ThemeSwitcher Themes => _themes ?? throw new InvalidOperationException("The themes are registered in Initialize.");

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        _themes = new ThemeSwitcher(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var services = AppComposition.Build();
            desktop.Exit += (_, _) => services.Dispose();
            if (!services.GetRequiredService<IWindowLock>().TryTake())
            {
                desktop.MainWindow = new AlreadyOpenWindow(); // Two windows on one library could undo each other's changes (ADR-0044).
                base.OnFrameworkInitializationCompleted();
                return;
            }

            FriendlyTimeConverter.Clock = services.GetRequiredService<TimeProvider>(); // Times read "Today, 18:05" (docs/VOICE.md).
            var window = services.GetRequiredService<MainWindowViewModel>();
            var looks = services.GetRequiredService<LookPresenter>();
            window.Profiles.SelectionChanged += (_, profile) => _ = looks.ShowForProfileAsync(profile); // A profile can have its own look (ADR-0017).
            desktop.MainWindow = new MainWindow { DataContext = window };
            services.GetRequiredService<StallWatch>().Start();
            _ = services.GetRequiredService<ThemePreference>().ApplySavedAsync();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
