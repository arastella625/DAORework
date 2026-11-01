using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using DAORework.App.Themes;
using DAORework.App.ViewModels;
using DAORework.App.Views;
using Xunit;

namespace DAORework.App.Tests.Themes;

/// <summary>Switching theme restyles an open window, and the Mods page in each theme is a screenshot for the PR (ADR-0016).</summary>
public sealed class ThemeSwitcherTests : IDisposable
{
    private readonly AppTestHost _host = new();

    private static ThemeSwitcher Switcher => ((App)Avalonia.Application.Current!).Themes; // The headless host runs App.

    public void Dispose()
    {
        Switcher.Apply(AppThemes.Default);
        _host.Dispose();
    }

    [AvaloniaFact]
    public void App_StartsInTheDefaultTheme()
    {
        Assert.Same(AppThemes.Default, Switcher.Current);
        Assert.Equal(AppThemes.Default.Variant, Avalonia.Application.Current!.ActualThemeVariant);
    }

    [AvaloniaTheory]
    [MemberData(nameof(ThemeDictionaries.Ids), MemberType = typeof(ThemeDictionaries))]
    public async Task Apply_RestylesAnOpenWindow(string id)
    {
        var theme = ThemeDictionaries.ById(id);
        _host.SeedRealisticInstall();
        var (window, viewModel) = await OpenAsync();
        viewModel.Profiles.NewProfileName = "Wilds run";
        await viewModel.Profiles.CreateProfileCommand.ExecuteAsync(null);
        await viewModel.SelectProfileAsync("Wilds run", TestContext.Current.CancellationToken);
        var pages = window.FindControl<TabControl>("Pages") ?? throw new InvalidOperationException("No tab control named Pages.");
        pages.SelectedIndex = MainWindowViewModel.ModsPage;
        await viewModel.Mods.InspectCommand.ExecuteAsync(null);

        Switcher.Apply(theme);

        Assert.Equal(ThemeDictionaries.ColorOf(theme, "Background"), BrushColor(window, "BackgroundBrush"));
        Assert.Equal(ThemeDictionaries.ColorOf(theme, "Accent"), BrushColor(window, "AccentButtonBackground"));
        Assert.Equal(ThemeDictionaries.ColorOf(theme, "Surface"), BrushColor(window, "SystemControlBackgroundChromeMediumLowBrush")); // Fluent's palette: the header.
        Screenshots.Save(window, $"20-theme-{id}");
    }

    private static Color BrushColor(Window window, string key) =>
        window.TryFindResource(key, window.ActualThemeVariant, out var value) && value is ISolidColorBrush brush
            ? brush.Color
            : throw new InvalidOperationException($"No brush {key} in theme {window.ActualThemeVariant}.");

    private async Task<(MainWindow Window, MainWindowViewModel ViewModel)> OpenAsync()
    {
        var viewModel = _host.Resolve<MainWindowViewModel>();
        var window = new MainWindow { DataContext = viewModel };
        window.Show();
        await viewModel.InitializeAsync(TestContext.Current.CancellationToken);
        return (window, viewModel);
    }
}
