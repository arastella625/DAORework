namespace DAORework.Formats.Erf;

/// <summary>What an ERF archive holds, from its header alone. <see cref="Flags"/> is zero for version 2.0.</summary>
public sealed record ErfTableOfContents(string Version, uint Flags, IReadOnlyList<ErfEntry> Entries)
{
    /// <summary>Gets a value indicating whether the entries' data is encrypted, which a reader of names needn't care about.</summary>
    public bool IsEncrypted => ((Flags >> 4) & 0xF) != 0;
}
