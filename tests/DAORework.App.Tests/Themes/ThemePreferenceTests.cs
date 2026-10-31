using Avalonia.Headless.XUnit;
using DAORework.App.Themes;
using Xunit;

namespace DAORework.App.Tests.Themes;

/// <summary>The saved theme is applied at start, and a pick is shown and saved (ADR-0016, ADR-0017).</summary>
public sealed class ThemePreferenceTests : IDisposable
{
    private readonly AppTestHost _host = new();

    private static ThemeSwitcher Switcher => ((App)Avalonia.Application.Current!).Themes; // The headless host runs App.

    public void Dispose()
    {
        Switcher.Apply(AppThemes.Default);
        _host.Dispose();
    }

    [AvaloniaTheory]
    [InlineData("{ \"schemaVersion\": 1, \"look\": \"the-fade\" }", "the-fade")]
    [InlineData("{ \"schemaVersion\": 1, \"look\": \"a-theme-from-a-later-version\" }", "witch-of-the-wilds")]
    [InlineData("not json", "witch-of-the-wilds")]
    [InlineData(null, "witch-of-the-wilds")]
    public async Task ApplySaved_UsesTheSavedTheme_OrTheDefault(string? file, string expected)
    {
        Switcher.Apply(AppThemes.Korcari);
        if (file is not null)
        {
            _host.FileSystem.AddFile(_host.PreferencesFile, new(file));
        }

        await _host.Resolve<ThemePreference>().ApplySavedAsync(TestContext.Current.CancellationToken);

        Assert.Equal(expected, Switcher.Current.Id);
    }

    [AvaloniaFact]
    public async Task Choose_ShowsTheThemeAtOnce_AndTheNextStartAppliesIt()
    {
        var preference = _host.Resolve<ThemePreference>();

        var saved = await preference.ChooseAsync(AppThemes.BloodAndShadow, TestContext.Current.CancellationToken);
        Assert.Same(AppThemes.BloodAndShadow, Switcher.Current);
        Switcher.Apply(AppThemes.Default);
        await preference.ApplySavedAsync(TestContext.Current.CancellationToken);

        Assert.True(saved.IsSuccess);
        Assert.Same(AppThemes.BloodAndShadow, Switcher.Current);
        Assert.Contains("blood-and-shadow", _host.FileSystem.File.ReadAllText(_host.PreferencesFile), StringComparison.Ordinal);
    }
}
