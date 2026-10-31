using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using DAORework.App.Controls;
using DAORework.App.Themes;
using Xunit;

namespace DAORework.App.Tests.Themes;

/// <summary>
/// The shared look (ADR-0016) on one sample screen laid out like the design mock: a page title with its rune, framed
/// cards, the Installed by hand list, severity rows and both confirm dialogs. One screenshot per theme for the PR.
/// The pages themselves move onto these styles in their own PR.
/// </summary>
public sealed class LookSampleTests : IDisposable
{
    private static ThemeSwitcher Switcher => ((App)Avalonia.Application.Current!).Themes; // The headless host runs App.

    public void Dispose() => Switcher.Apply(AppThemes.Default);

    [AvaloniaFact]
    public void HeadingFont_IsTheShippedCinzel()
    {
        var family = (FontFamily)Avalonia.Application.Current!.FindResource("HeadingFontFamily")!; // Defined in Shared.axaml.

        Assert.True(FontManager.Current.TryGetGlyphTypeface(new Typeface(family), out var glyphs));
        Assert.Equal("Cinzel", glyphs.FamilyName);
    }

    [AvaloniaTheory]
    [MemberData(nameof(ThemeDictionaries.Ids), MemberType = typeof(ThemeDictionaries))]
    public void SampleScreen_RendersInEveryTheme(string id)
    {
        var theme = ThemeDictionaries.ById(id);
        Switcher.Apply(theme);
        var title = LookSample.Text("Mods", "PageTitle");
        var window = new Window { Width = 1000, Height = 1160, Content = new Backdrop { Padding = new(24, 20), Content = LookSample.Build(title) } };
        window.Show();

        Assert.Equal(ThemeDictionaries.ColorOf(theme, "Heading"), Assert.IsAssignableFrom<ISolidColorBrush>(title.Foreground).Color);
        Screenshots.Save(window, $"21-look-{id}");
        window.Close();
    }
}
