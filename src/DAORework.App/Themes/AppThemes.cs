using Avalonia.Styling;

namespace DAORework.App.Themes;

/// <summary>The four built-in themes (ADR-0016). All are dark, so Fluent's own dark resources sit underneath.</summary>
public static class AppThemes
{
    public static AppTheme WitchOfTheWilds { get; } = Create(
        "witch-of-the-wilds", "Witch of the Wilds", "Violet and gold, a soft glow on what you can touch, red only for danger.", nameof(WitchOfTheWilds));

    public static AppTheme Korcari { get; } = Create(
        "korcari", "Korcari", "The same colors with no glow and a plain gold rule.", nameof(Korcari));

    public static AppTheme TheFade { get; } = Create(
        "the-fade", "The Fade", "Cooler lavender, misty, with the strongest glow.", nameof(TheFade));

    public static AppTheme BloodAndShadow { get; } = Create(
        "blood-and-shadow", "Blood and Shadow", "Iron, leather and bronze, with crimson kept for emphasis and danger.", nameof(BloodAndShadow));

    /// <summary>Gets the theme a new user sees, and the one an unknown saved id falls back to.</summary>
    public static AppTheme Default => WitchOfTheWilds;

    public static IReadOnlyList<AppTheme> All { get; } = [WitchOfTheWilds, Korcari, TheFade, BloodAndShadow];

    /// <returns>The theme with <paramref name="id"/>, or null for an id this build does not have (a removed theme).</returns>
    public static AppTheme? Find(string? id) => All.FirstOrDefault(theme => theme.Id == id);

    private static AppTheme Create(string id, string name, string description, string key) =>
        new(id, name, description, new ThemeVariant(key, ThemeVariant.Dark), new Uri($"avares://DAORework.App/Themes/{key}.axaml"));
}
