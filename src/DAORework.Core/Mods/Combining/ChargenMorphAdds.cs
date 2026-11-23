namespace DAORework.Core.Mods.Combining;

/// <summary>
/// What a mod, or a change, adds to the character creator (ADR-0050 §4), in the words of docs/VOICE.md. Counts are never
/// guessed: without a list to count against, it only says it adds to character creation.
/// </summary>
public sealed record ChargenMorphAdds(IReadOnlyDictionary<ChargenMorphKind, int> Counts, bool Counted)
{
    private static readonly (ChargenMorphKind Kind, string One, string Many)[] _words =
    [
        (ChargenMorphKind.Hairstyle, "hairstyle", "hairstyles"),
        (ChargenMorphKind.Face, "face", "faces"),
        (ChargenMorphKind.Beard, "beard", "beards"),
        (ChargenMorphKind.Colour, "colour", "colours"),
        (ChargenMorphKind.Tattoo, "tattoo", "tattoos"),
        (ChargenMorphKind.Skin, "skin", "skins"),
    ];

    /// <summary>Gets a mod with a copy but nothing to count it against.</summary>
    public static ChargenMorphAdds Uncounted { get; } = new(new Dictionary<ChargenMorphKind, int>(), Counted: false);

    /// <summary>For a mod's row: <i>Adds 12 hairstyles</i>; null when it adds nothing new.</summary>
    public string? Adds()
    {
        if (!Counted)
        {
            return "Adds to character creation";
        }

        var named = Named(static (count, words) => $"{count} {words}");
        return named is not null ? "Adds " + named
            : Counts.GetValueOrDefault(ChargenMorphKind.Other) > 0 ? "Adds to character creation"
            : null;
    }

    /// <summary>After a change: <i>Character creation has 48 more hairstyles</i>; null when nothing can be said in counts.</summary>
    public string? Gained() =>
        Counted && Named(static (count, words) => $"{count} more {words}") is { } named ? "Character creation has " + named : null;

    // The kinds with a word, as one list: "3 faces and 8 colours"; null when none was added.
    private string? Named(Func<int, string, string> each)
    {
        var named = _words.Where(w => Counts.GetValueOrDefault(w.Kind) > 0)
            .Select(w => Counts[w.Kind] == 1 ? each(1, w.One) : each(Counts[w.Kind], w.Many))
            .ToList();
        return named.Count switch
        {
            0 => null,
            1 => named[0],
            _ => $"{string.Join(", ", named[..^1])} and {named[^1]}",
        };
    }
}
