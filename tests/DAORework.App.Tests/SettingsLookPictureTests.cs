using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media.Imaging;
using DAORework.App.Tests.Themes;
using DAORework.App.Themes;
using DAORework.App.ViewModels;
using DAORework.App.Views;
using DAORework.Core.Diagnostics;
using DAORework.Core.Looks;
using Xunit;

namespace DAORework.App.Tests;

/// <summary>A look's own picture on the Settings page (ADR-0017): choose it, blend it in, take it away.</summary>
public sealed class SettingsLookPictureTests : IDisposable
{
    private const int SettingsTab = MainWindowViewModel.SettingsPage;

    private readonly AppTestHost _host = new();

    private static ThemeSwitcher Switcher => ((App)Avalonia.Application.Current!).Themes; // The headless host runs App.

    private static Avalonia.Controls.IResourceDictionary Resources => Avalonia.Application.Current!.Resources;

    public void Dispose()
    {
        Switcher.Apply(AppThemes.Default);
        _host.Dispose();
    }

    [AvaloniaFact]
    public async Task Choose_AddsThePicture_ShowsItUnderTheVeil_AndNamesTheFloor()
    {
        var (window, looks, picture) = await OpenWithLookAsync();
        _host.Picker.Enqueue(DuskFile());

        await picture.ChooseCommand.ExecuteAsync(null);

        Assert.False(picture.IsError, picture.StatusMessage);
        Assert.True(picture.HasPicture);
        Assert.NotNull(looks.Looks[0].Background);
        Assert.IsType<WriteableBitmap>(Resources[LookPictureLayer.PictureKey]);
        Assert.InRange(picture.Floor, 1, 100); // The dusk picture's moon is near white, so some veil is needed.
        Assert.Contains($"{picture.Floor}%", picture.FloorNote, StringComparison.Ordinal);
        Assert.True((double)Resources[LookPictureLayer.VeilKey]! >= picture.Floor / 100d);
        window.Height = 1460; // The theme picker, the looks, the editor and the picture's controls in one shot.
        window.UpdateLayout();
        Screenshots.Save(window, "26-settings-look-picture");
    }

    [AvaloniaFact]
    public async Task Choose_Cancelled_ChangesNothing()
    {
        var (_, looks, picture) = await OpenWithLookAsync();

        await picture.ChooseCommand.ExecuteAsync(null); // The fake picker has no answer queued: the user cancelled.

        Assert.False(picture.HasPicture);
        Assert.Null(looks.Looks[0].Background);
        Assert.Equal(string.Empty, picture.StatusMessage);
    }

    [AvaloniaFact]
    public async Task Choose_AFileThatIsNotAPicture_SaysWhy()
    {
        var (_, _, picture) = await OpenWithLookAsync();
        var path = Path.Combine(Path.GetDirectoryName(_host.PreferencesFile)!, "notes.png");
        _host.FileSystem.AddFile(path, new("not a picture"));
        _host.Picker.Enqueue(path);

        await picture.ChooseCommand.ExecuteAsync(null);

        Assert.True(picture.IsError);
        Assert.Contains(DiagnosticCodes.LookImageUnreadable, picture.StatusMessage, StringComparison.Ordinal);
        Assert.False(picture.HasPicture);
    }

    [AvaloniaFact]
    public async Task Settings_SaveOnceSettled_AndTheVeilIsKeptAtTheFloor()
    {
        var (_, looks, picture) = await OpenWithLookAsync();
        _host.Picker.Enqueue(DuskFile());
        await picture.ChooseCommand.ExecuteAsync(null);

        picture.FillWindow = false;
        picture.Tint = 70;
        picture.Blur = 12;
        picture.Dim = 0;
        await picture.Settled;

        var saved = looks.Looks[0].Background!;
        Assert.Equal((BackgroundFit.Fit, 70, 12), (saved.Fit, saved.Tint, saved.Blur));
        Assert.Equal(picture.Floor, saved.Dim); // Asked for no veil; kept at the floor so text stays readable.
        Assert.Equal(Avalonia.Media.Stretch.Uniform, Resources[LookPictureLayer.StretchKey]);
    }

    [AvaloniaFact]
    public async Task Remove_TakesThePictureAway()
    {
        var (_, looks, picture) = await OpenWithLookAsync();
        _host.Picker.Enqueue(DuskFile());
        await picture.ChooseCommand.ExecuteAsync(null);

        await picture.RemoveCommand.ExecuteAsync(null);

        Assert.False(picture.HasPicture);
        Assert.Null(looks.Looks[0].Background);
        Assert.Null(Resources[LookPictureLayer.PictureKey]);
        Assert.Equal(looks.Looks[0].Id, Switcher.CurrentLookId); // Still the look, now without its picture.
    }

    private string DuskFile()
    {
        var path = Path.Combine(Path.GetDirectoryName(_host.PreferencesFile)!, "dusk.png");
        _host.FileSystem.AddFile(path, new(TestPictures.Dusk()));
        return path;
    }

    private async Task<(MainWindow Window, LookListViewModel Looks, LookPictureViewModel Picture)> OpenWithLookAsync()
    {
        var viewModel = _host.Resolve<MainWindowViewModel>();
        var window = new MainWindow { DataContext = viewModel };
        window.Show();
        await viewModel.InitializeAsync(TestContext.Current.CancellationToken);
        var tabs = window.FindControl<TabControl>("Pages") ?? throw new InvalidOperationException("No tab control named Pages.");
        tabs.SelectedIndex = SettingsTab;
        var looks = viewModel.Settings.Looks;
        await looks.NewLookCommand.ExecuteAsync(null);
        await viewModel.Settings.Picture.Settled;
        viewModel.Settings.Details.PickAccentCommand.Execute("#3BA3D9");
        await viewModel.Settings.Details.SaveCommand.ExecuteAsync(null);
        return (window, looks, viewModel.Settings.Picture);
    }
}
