using System.Xml;
using System.Xml.Linq;

namespace DAORework.Core.Mods.Combining;

/// <summary>
/// How <c>chargenmorphcfg.xml</c> is read and what an entry is, shared by combining (ADR-0025) and counting (ADR-0050 §4).
/// An entry is an element without child elements; two are the same when the name and every attribute match, without
/// letter case. No DTDs and no external resources: a mod's file can never make DAORework read anything else.
/// </summary>
internal static class ChargenMorphXml
{
    private const long MaxCharacters = 16 * 1024 * 1024;

    /// <summary>The root, whitespace dropped so a combined file is indented the same throughout.</summary>
    /// <exception cref="XmlException">The text isn't well-formed XML.</exception>
    public static XElement? Load(string xml)
    {
        var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = MaxCharacters, IgnoreWhitespace = true };
        using var reader = XmlReader.Create(new StringReader(xml), settings);
        return XDocument.Load(reader).Root;
    }

    public static bool IsEntry(XElement element) => !element.HasElements;

    public static bool SameName(XElement a, XElement b) => a.Name.LocalName.Equals(b.Name.LocalName, StringComparison.OrdinalIgnoreCase);

    public static bool SameEntry(XElement a, XElement b) => SameName(a, b) && Attributes(a).SetEquals(Attributes(b));

    /// <summary>An entry's name and attributes as one text, the same for two entries <see cref="SameEntry"/> calls the same.</summary>
    public static string Key(XElement entry) =>
        entry.Name.LocalName.ToUpperInvariant() + "|" + string.Join("|", Attributes(entry).Order(StringComparer.Ordinal));

    private static HashSet<string> Attributes(XElement element) =>
        element.Attributes().Select(x => $"{x.Name.LocalName.ToUpperInvariant()}={x.Value.ToUpperInvariant()}").ToHashSet(StringComparer.Ordinal);
}
