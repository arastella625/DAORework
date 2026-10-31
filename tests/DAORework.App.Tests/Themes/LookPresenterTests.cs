using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using DAORework.App.Themes;
using DAORework.App.ViewModels;
using DAORework.App.Views;
using DAORework.Application.Looks;
using DAORework.Core.Looks;
using Xunit;

namespace DAORework.App.Tests.Themes;

/// <summary>A custom look on screen (ADR-0017): its accent, its picture under the veil, and the look that follows the profile.</summary>
public sealed class LookPresenterTests : IDisposable
{
    private const string Profile = "Wilds run";

    private readonly AppTestHost _host = new();

    private static ThemeSwitcher Switcher => ((App)Avalonia.Application.Current!).Themes; // The headless host runs App.

    private static Avalonia.Controls.IResourceDictionary Resources => Avalonia.Application.Current!.Resources;

    public void Dispose()
    {
        Switcher.Apply(AppThemes.Default);
        _host.Dispose();
    }

    [AvaloniaFact]
    public async Task AProfilesOwnLook_ShowsItsAccentAndItsPictureUnderTheVeil()
    {
        var look = await MakeLookAsync(accent: "#3BA3D9", withPicture: true);
        var (window, _) = await OpenModsAsync();

        await _host.Resolve<LookPresenter>().ShowForProfileAsync(Profile, TestContext.Current.CancellationToken);

        Assert.Equal(look.Id, Switcher.CurrentLookId);
        Assert.True(window.TryFindResource(ThemeTokens.BrushKey("Accent"), window.ActualThemeVariant, out var brush), $"No accent in {window.ActualThemeVariant}.");
        var accent = Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color;
        Assert.InRange(accent.ToHsl().H, 199, 201); // The pick's hue, #3BA3D9, kept.
        Assert.IsType<WriteableBitmap>(Resources[LookPictureLayer.PictureKey]);
        var floor = DimFloorOf(look);
        Assert.True((double)Resources[LookPictureLayer.VeilKey]! >= floor / 100d, $"The veil is below the floor of {floor}%.");
        Screenshots.Save(window, "24-look-with-picture");
    }

    [AvaloniaFact]
    public async Task AProfileWithoutItsOwnLook_ShowsTheAppWideTheme_WithNoPicture()
    {
        await MakeLookAsync(accent: null, withPicture: true);

        await _host.Resolve<LookPresenter>().ShowForProfileAsync("Another run", TestContext.Current.CancellationToken);

        Assert.Null(Switcher.CurrentLookId);
        Assert.Same(AppThemes.Default, Switcher.Current);
        Assert.Null(Resources[LookPictureLayer.PictureKey]);
        Assert.Equal(0d, Resources[LookPictureLayer.VeilKey]);
    }

    [AvaloniaFact]
    public async Task ALookThatIsGone_FallsBackToTheDefault()
    {
        await _host.Resolve<SetProfileLook>().ExecuteAsync(new SetProfileLookRequest(Profile, "look-deleted"), TestContext.Current.CancellationToken);
        Switcher.Apply(AppThemes.TheFade);

        await _host.Resolve<LookPresenter>().ShowForProfileAsync(Profile, TestContext.Current.CancellationToken);

        Assert.Same(AppThemes.Default, Switcher.Current);
        Assert.Null(Switcher.CurrentLookId);
    }

    [AvaloniaFact]
    public async Task ALookWhosePictureIsGone_IsShownWithoutIt()
    {
        var look = await MakeLookAsync(accent: null, withPicture: true);
        _host.FileSystem.File.Delete(Path.Combine(Path.GetDirectoryName(_host.PreferencesFile)!, "looks", look.Id, look.Background!.FileName));

        await _host.Resolve<LookPresenter>().ShowForProfileAsync(Profile, TestContext.Current.CancellationToken);

        Assert.Equal(look.Id, Switcher.CurrentLookId);
        Assert.Null(Resources[LookPictureLayer.PictureKey]);
    }

    private async Task<Look> MakeLookAsync(string? accent, bool withPicture)
    {
        var token = TestContext.Current.CancellationToken;
        var look = (await _host.Resolve<CreateLook>().ExecuteAsync(new CreateLookRequest("Dusk in the Wilds", AppThemes.WitchOfTheWilds.Id), token)).Value;
        look = (await _host.Resolve<UpdateLook>().ExecuteAsync(new UpdateLookRequest(look.Id, look.Name, look.BaseTheme, accent), token)).Value;
        if (withPicture)
        {
            var picture = Path.Combine(Path.GetDirectoryName(_host.PreferencesFile)!, "dusk.png");
            _host.FileSystem.AddFile(picture, new(TestPictures.Dusk()));
            look = (await _host.Resolve<SetLookBackground>().ExecuteAsync(new SetLookBackgroundRequest(look.Id, picture), token)).Value;
            look = (await _host.Resolve<AdjustLookBackground>().ExecuteAsync(new AdjustLookBackgroundRequest(look.Id, BackgroundFit.Fill, 40, 0, 4, 0), token)).Value;
        }

        await _host.Resolve<SetProfileLook>().ExecuteAsync(new SetProfileLookRequest(Profile, look.Id), token);
        return look;
    }

    // The floor the presenter must keep: computed the way it does, from the look's own colors.
    private static int DimFloorOf(Look look)
    {
        var (dictionary, _) = LookAppearance.Build(Switcher.DictionaryOf(AppThemes.Find(look.BaseTheme)!), look);
        return PictureTreatment.DimFloor(
            LookColor.Parse(look.Background!.Brightest).Value,
            LookAppearance.ToLook(dictionary, "Background"),
            LookAppearance.ToLook(dictionary, "Accent"),
            look.Background.Tint,
            LookAppearance.TextRequirements(dictionary));
    }

    private async Task<(MainWindow Window, MainWindowViewModel ViewModel)> OpenModsAsync()
    {
        var viewModel = _host.Resolve<MainWindowViewModel>();
        var window = new MainWindow { DataContext = viewModel };
        window.Show();
        await viewModel.InitializeAsync(TestContext.Current.CancellationToken);
        viewModel.Profiles.NewProfileName = Profile;
        await viewModel.Profiles.CreateProfileCommand.ExecuteAsync(null);
        await viewModel.SelectProfileAsync(Profile, TestContext.Current.CancellationToken);
        var pages = window.FindControl<TabControl>("Pages") ?? throw new InvalidOperationException("No tab control named Pages.");
        pages.SelectedIndex = 2;
        return (window, viewModel);
    }
}
