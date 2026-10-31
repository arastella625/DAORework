using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using DAORework.App.Themes;
using DAORework.App.ViewModels;
using DAORework.App.Views;
using DAORework.Application.Preferences;
using DAORework.Core.Preferences;
using Xunit;

namespace DAORework.App.Tests;

/// <summary>Choosing a launch profile's own look on the Profile page (ADR-0017).</summary>
public sealed class ProfileLookTests : IDisposable
{
    private const string Profile = "Wilds run";

    private readonly AppTestHost _host = new();

    private static ThemeSwitcher Switcher => ((App)Avalonia.Application.Current!).Themes; // The headless host runs App.

    public void Dispose()
    {
        Switcher.Apply(AppThemes.Default);
        _host.Dispose();
    }

    [AvaloniaFact]
    public async Task ALookMadeInSettings_IsOffered_AndChoosingItSavesAndShowsIt()
    {
        var (window, viewModel) = await OpenAsync();
        await viewModel.Settings.Looks.NewLookCommand.ExecuteAsync(null);
        var made = viewModel.Settings.Looks.Looks[0];
        var look = viewModel.Profile.Look;
        await Refreshed(look, made.Id);

        await look.ChooseCommand.ExecuteAsync(look.Choices.Single(choice => choice.Id == made.Id));

        Assert.False(look.IsError, look.StatusMessage);
        Assert.Equal(made.Id, (await PreferencesAsync()).ProfileLooks[Profile]);
        Assert.Equal(made.Id, Switcher.CurrentLookId);
        Assert.Equal(made.Id, look.SelectedChoice?.Id);
        Screenshots.Save(window, "27-profile-look");
    }

    [AvaloniaFact]
    public async Task ABuiltInTheme_CanBeAProfilesLook_AndTheAppWideChoiceClearsIt()
    {
        var (_, viewModel) = await OpenAsync();
        var look = viewModel.Profile.Look;

        await look.ChooseCommand.ExecuteAsync(look.Choices.Single(choice => choice.Id == AppThemes.BloodAndShadow.Id));
        Assert.Same(AppThemes.BloodAndShadow, Switcher.Current);

        await look.ChooseCommand.ExecuteAsync(look.Choices[0]);

        Assert.False((await PreferencesAsync()).ProfileLooks.ContainsKey(Profile));
        Assert.Same(AppThemes.Default, Switcher.Current);
    }

    [AvaloniaFact]
    public async Task SelectingAProfile_ShowsItsSavedChoice()
    {
        var (_, viewModel) = await OpenAsync();
        await viewModel.Profile.Look.ChooseCommand.ExecuteAsync(viewModel.Profile.Look.Choices.Single(choice => choice.Id == AppThemes.TheFade.Id));
        viewModel.Profiles.NewProfileName = "Another run";
        await viewModel.Profiles.CreateProfileCommand.ExecuteAsync(null);
        await viewModel.SelectProfileAsync("Another run", TestContext.Current.CancellationToken);
        Assert.Null(viewModel.Profile.Look.SelectedChoice?.Id);

        await viewModel.SelectProfileAsync(Profile, TestContext.Current.CancellationToken);

        Assert.Equal(AppThemes.TheFade.Id, viewModel.Profile.Look.SelectedChoice?.Id);
    }

    // Making a look in Settings starts the Profile page's refresh without waiting; let it finish.
    private static async Task Refreshed(ProfileLookViewModel look, string lookId)
    {
        for (var i = 0; i < 50 && look.Choices.All(choice => choice.Id != lookId); i++)
        {
            await Task.Delay(10, TestContext.Current.CancellationToken);
        }
    }

    private async Task<AppPreferences> PreferencesAsync() =>
        (await _host.Resolve<GetPreferences>().ExecuteAsync(new GetPreferencesRequest(), TestContext.Current.CancellationToken)).Value;

    private async Task<(MainWindow Window, MainWindowViewModel ViewModel)> OpenAsync()
    {
        var viewModel = _host.Resolve<MainWindowViewModel>();
        var window = new MainWindow { DataContext = viewModel };
        window.Show();
        await viewModel.InitializeAsync(TestContext.Current.CancellationToken);
        viewModel.Profiles.NewProfileName = Profile;
        await viewModel.Profiles.CreateProfileCommand.ExecuteAsync(null);
        await viewModel.SelectProfileAsync(Profile, TestContext.Current.CancellationToken);
        var tabs = window.FindControl<TabControl>("Pages") ?? throw new InvalidOperationException("No tab control named Pages.");
        tabs.SelectedIndex = 0;
        return (window, viewModel);
    }
}
