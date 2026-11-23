namespace DAORework.Core.Mods.Combining;

/// <summary>What a section of <c>chargenmorphcfg.xml</c> offers in the character creator (ADR-0050 §4, docs/VOICE.md).</summary>
public enum ChargenMorphKind
{
    Hairstyle,
    Face,
    Beard,
    Colour,
    Tattoo,
    Skin,

    /// <summary>A section DAORework has no word for: counted, but only said as <i>Adds to character creation</i>.</summary>
    Other,
}
