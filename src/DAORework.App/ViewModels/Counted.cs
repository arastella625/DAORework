namespace DAORework.App.ViewModels;

/// <summary>A count as a person writes it (docs/VOICE.md rule 5): <i>1 file</i>, <i>2 files</i>, never <i>file(s)</i>.</summary>
public static class Counted
{
    /// <summary><paramref name="count"/> and <paramref name="one"/>, or <paramref name="many"/> (default: <paramref name="one"/> plus "s").</summary>
    public static string Of(int count, string one, string? many = null) =>
        count == 1 ? $"1 {one}" : $"{count} {many ?? one + "s"}";
}
