using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using DAORework.App.Themes;
using DAORework.App.ViewModels;
using DAORework.App.Views;
using DAORework.Application.Preferences;
using DAORework.Core.Diagnostics;
using Xunit;

namespace DAORework.App.Tests;

/// <summary>Making and editing custom looks on the Settings page (ADR-0017).</summary>
public sealed class SettingsLooksTests : IDisposable
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
    public async Task NewLook_StartsFromTheCurrentTheme_AndIsPreviewed()
    {
        var (_, looks, details) = await OpenAsync();

        await looks.NewLookCommand.ExecuteAsync(null);

        var look = Assert.Single(looks.Looks);
        Assert.Same(look, looks.SelectedLook);
        Assert.Equal(AppThemes.Default.Id, look.BaseTheme);
        Assert.Equal(look.Name, details.Name);
        Assert.True(details.UseThemeAccent);
    }

    [AvaloniaFact]
    public async Task Save_KeepsTheNameThemeAndAccent_AndShowsTheLook()
    {
        var (window, looks, details) = await OpenAsync();
        await looks.NewLookCommand.ExecuteAsync(null);
        await Previewed(looks);

        details.Name = "Wilds run";
        details.BaseTheme = AppThemes.TheFade;
        details.PickAccentCommand.Execute("#3BA3D9");
        await details.SaveCommand.ExecuteAsync(null);

        Assert.Equal((false, "Saved Wilds run."), (details.IsError, details.StatusMessage));
        Assert.Equal("Wilds run", Assert.Single(looks.Looks).Name);
        Assert.Equal(("the-fade", "#3BA3D9"), (looks.Looks[0].BaseTheme, looks.Looks[0].Accent));
        Assert.Equal(looks.Looks[0].Id, Switcher.CurrentLookId);
        window.Height = 1080; // Tall enough to show the theme picker, the looks and the editor in one shot.
        window.UpdateLayout();
        Screenshots.Save(window, "25-settings-looks");
    }

    [AvaloniaFact]
    public async Task Save_ARedAccent_SaysWhyAndSavesNothing()
    {
        var (_, looks, details) = await OpenAsync();
        await looks.NewLookCommand.ExecuteAsync(null);

        details.PickAccentCommand.Execute("#C42B1C");
        await details.SaveCommand.ExecuteAsync(null);

        Assert.True(details.IsError);
        Assert.Contains(DiagnosticCodes.LookAccentReserved, details.StatusMessage, StringComparison.Ordinal);
        Assert.Null(looks.Looks[0].Accent);
    }

    [AvaloniaFact]
    public async Task Delete_TakesASecondClick_ThenGoesBackToTheDefault()
    {
        var (_, looks, _) = await OpenAsync();
        await looks.NewLookCommand.ExecuteAsync(null);
        await looks.UseEverywhereCommand.ExecuteAsync(null);

        await looks.DeleteCommand.ExecuteAsync(null);
        Assert.Single(looks.Looks);
        Assert.True(looks.ConfirmingDelete);
        await looks.DeleteCommand.ExecuteAsync(null);

        Assert.Empty(looks.Looks);
        Assert.Null(Switcher.CurrentLookId);
        Assert.Same(AppThemes.Default, Switcher.Current);
    }

    [AvaloniaFact]
    public async Task UseEverywhere_MakesTheLookTheAppWideOne()
    {
        var (_, looks, _) = await OpenAsync();
        await looks.NewLookCommand.ExecuteAsync(null);

        await looks.UseEverywhereCommand.ExecuteAsync(null);

        var preferences = (await _host.Resolve<GetPreferences>().ExecuteAsync(new GetPreferencesRequest(), TestContext.Current.CancellationToken)).Value;
        Assert.Equal(looks.Looks[0].Id, preferences.Look);
        Assert.Equal(looks.Looks[0].Id, Switcher.CurrentLookId);
    }

    // Picking a look starts its preview without waiting; let it finish before the test applies anything else.
    private static async Task Previewed(LookListViewModel looks)
    {
        for (var i = 0; i < 50 && Switcher.CurrentLookId != looks.SelectedLook?.Id; i++)
        {
            await Task.Delay(10, TestContext.Current.CancellationToken);
        }
    }

    private async Task<(MainWindow Window, LookListViewModel Looks, LookDetailsViewModel Details)> OpenAsync()
    {
        var viewModel = _host.Resolve<MainWindowViewModel>();
        var window = new MainWindow { DataContext = viewModel };
        window.Show();
        await viewModel.InitializeAsync(TestContext.Current.CancellationToken);
        var tabs = window.FindControl<TabControl>("Pages") ?? throw new InvalidOperationException("No tab control named Pages.");
        tabs.SelectedIndex = SettingsTab;
        window.UpdateLayout();
        return (window, viewModel.Settings.Looks, viewModel.Settings.Details);
    }
}
