using System.Xml;
using System.Xml.Linq;

namespace DAORework.Core.Mods.Combining;

/// <summary>
/// The entries one copy of <c>chargenmorphcfg.xml</c> offers, each with what it is (ADR-0050 §4). An entry is the same
/// as another when it sits under sections of the same names and <see cref="ChargenMorphXml.SameEntry"/> says so. Pure.
/// </summary>
public sealed class ChargenMorphEntries
{
    // shortcut: section names as ADR-0025 and the toolset wiki give them, not yet checked against a real game file;
    // confirm against the owner's copy before P2.10-03 leaves draft.
    private static readonly (string Section, ChargenMorphKind Kind)[] _kinds =
    [
        ("hairs", ChargenMorphKind.Hairstyle),
        ("heads", ChargenMorphKind.Face),
        ("beards", ChargenMorphKind.Beard),
        ("tattoos", ChargenMorphKind.Tattoo),
        ("skins", ChargenMorphKind.Skin),
    ];

    private readonly Dictionary<string, ChargenMorphKind> _entries;

    private ChargenMorphEntries(Dictionary<string, ChargenMorphKind> entries) => _entries = entries;

    /// <summary>Gets how many entries the copy offers.</summary>
    public int Count => _entries.Count;

    /// <summary>The entries of one copy, or null when it isn't a <c>morph_config</c> the game could read.</summary>
    public static ChargenMorphEntries? Read(string xml)
    {
        ArgumentNullException.ThrowIfNull(xml);
        try
        {
            var root = ChargenMorphXml.Load(xml);
            if (root is null || !root.Name.LocalName.Equals(ChargenMorphCombiner.RootName, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var entries = new Dictionary<string, ChargenMorphKind>(StringComparer.Ordinal);
            foreach (var section in root.Elements())
            {
                var kind = KindOf(section.Name.LocalName);
                foreach (var entry in section.DescendantsAndSelf().Where(ChargenMorphXml.IsEntry))
                {
                    entries.TryAdd(PathOf(entry) + ChargenMorphXml.Key(entry), kind);
                }
            }

            return new ChargenMorphEntries(entries);
        }
        catch (XmlException)
        {
            return null;
        }
    }

    /// <summary>The entries every copy has, the list mods add to when the game's own copy can't be read.</summary>
    public static ChargenMorphEntries Shared(IReadOnlyCollection<ChargenMorphEntries> copies)
    {
        ArgumentNullException.ThrowIfNull(copies);
        var shared = copies.Count == 0
            ? []
            : copies.Skip(1).Aggregate(
                new Dictionary<string, ChargenMorphKind>(copies.First()._entries, StringComparer.Ordinal),
                (common, copy) => common.Where(e => copy._entries.ContainsKey(e.Key)).ToDictionary(e => e.Key, e => e.Value, StringComparer.Ordinal));
        return new ChargenMorphEntries(shared);
    }

    /// <summary>The entries any of <paramref name="copies"/> offers.</summary>
    public static ChargenMorphEntries Union(IEnumerable<ChargenMorphEntries> copies)
    {
        ArgumentNullException.ThrowIfNull(copies);
        var all = new Dictionary<string, ChargenMorphKind>(StringComparer.Ordinal);
        foreach (var entry in copies.SelectMany(c => c._entries))
        {
            all.TryAdd(entry.Key, entry.Value);
        }

        return new ChargenMorphEntries(all);
    }

    /// <summary>How many of each kind this copy offers that <paramref name="baseline"/> doesn't.</summary>
    public IReadOnlyDictionary<ChargenMorphKind, int> Beyond(ChargenMorphEntries baseline)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        return _entries.Where(e => !baseline._entries.ContainsKey(e.Key))
            .GroupBy(e => e.Value)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    private static ChargenMorphKind KindOf(string section)
    {
        if (section.Contains("color", StringComparison.OrdinalIgnoreCase) || section.Contains("colour", StringComparison.OrdinalIgnoreCase))
        {
            return ChargenMorphKind.Colour;
        }

        return _kinds.FirstOrDefault(k => k.Section.Equals(section, StringComparison.OrdinalIgnoreCase)) is { Section: not null } known
            ? known.Kind
            : ChargenMorphKind.Other;
    }

    // The sections and groups above an entry, so the same hair under human_male and elf_male is two entries.
    private static string PathOf(XElement entry) =>
        string.Concat(entry.Ancestors().Reverse().Skip(1).Select(a => a.Name.LocalName.ToUpperInvariant() + "/"));
}
