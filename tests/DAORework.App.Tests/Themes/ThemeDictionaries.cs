using Avalonia.Controls;
using Avalonia.Media;
using DAORework.App.Themes;
using Xunit;

namespace DAORework.App.Tests.Themes;

/// <summary>Reads a registered theme back from the running application, as a view would see it.</summary>
internal static class ThemeDictionaries
{
    public static TheoryData<string> Ids => new(AppThemes.All.Select(theme => theme.Id));

    public static AppTheme ById(string id) => AppThemes.All.Single(theme => theme.Id == id);

    public static ResourceDictionary Of(AppTheme theme) =>
        (ResourceDictionary)Avalonia.Application.Current!.Resources.ThemeDictionaries[theme.Variant]; // The headless host is running.

    public static Color ColorOf(AppTheme theme, string token) => (Color)Of(theme)[ThemeTokens.ColorKey(token)]!; // Checked by ThemeTokenTests.
}
