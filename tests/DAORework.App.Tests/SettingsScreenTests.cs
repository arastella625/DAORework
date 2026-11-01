using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using DAORework.App.Themes;
using DAORework.App.ViewModels;
using DAORework.App.Views;
using Xunit;

namespace DAORework.App.Tests;

/// <summary>The Settings page's theme picker (ADR-0016): a pick shows at once and is saved; the saved theme shows as picked.</summary>
public sealed class SettingsScreenTests : IDisposable
{
    private const int SettingsTab = MainWindowViewModel.SettingsPage;

    private readonly AppTestHost _host = new();

    private static ThemeSwitcher Switcher => ((App)Avalonia.Application.Current!).Themes; // The headless host runs App.

    public void Dispose()
    {
        Switcher.Apply(AppThemes.Default);
        _host.Dispose();
    }

    [AvaloniaFact]
    public async Task SettingsPage_ListsTheFourThemes_WithTheDefaultPicked()
    {
        var (window, viewModel) = await OpenSettingsAsync();

        Assert.Equal(AppThemes.All, viewModel.Settings.Themes);
        Assert.Same(AppThemes.Default, viewModel.Settings.SelectedTheme);
        Screenshots.Save(window, "22-settings");
    }

    [AvaloniaFact]
    public async Task PickingATheme_ShowsItAtOnce_AndSavesIt()
    {
        var (window, viewModel) = await OpenSettingsAsync();

        viewModel.Settings.SelectedTheme = AppThemes.TheFade; // What a click on the card does.
        await viewModel.Settings.ChooseThemeCommand.ExecutionTask!; // Started by the selection.

        Assert.Same(AppThemes.TheFade, Switcher.Current);
        Assert.False(viewModel.Settings.IsError);
        Assert.Contains("The Fade", viewModel.Settings.StatusMessage, StringComparison.Ordinal);
        Assert.Contains("the-fade", _host.FileSystem.File.ReadAllText(_host.PreferencesFile), StringComparison.Ordinal);
        Screenshots.Save(window, "23-settings-the-fade");
    }

    [AvaloniaFact]
    public async Task AThemeAppliedElsewhere_ShowsAsPicked_WithoutSavingAgain()
    {
        var (_, viewModel) = await OpenSettingsAsync();

        Switcher.Apply(AppThemes.Korcari); // As the saved theme is applied at start.

        Assert.Same(AppThemes.Korcari, viewModel.Settings.SelectedTheme);
        Assert.False(_host.FileSystem.File.Exists(_host.PreferencesFile));
    }

    private async Task<(MainWindow Window, MainWindowViewModel ViewModel)> OpenSettingsAsync()
    {
        var viewModel = _host.Resolve<MainWindowViewModel>();
        var window = new MainWindow { DataContext = viewModel };
        window.Show();
        await viewModel.InitializeAsync(TestContext.Current.CancellationToken);
        var tabs = window.FindControl<TabControl>("Pages") ?? throw new InvalidOperationException("No tab control named Pages.");
        tabs.SelectedIndex = SettingsTab;
        return (window, viewModel);
    }
}
