using System.Text;
using System.Xml;
using System.Xml.Linq;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;

namespace DAORework.Core.Mods.Combining;

/// <summary>
/// Combines the <c>chargenmorphcfg.xml</c> copies of several mods into one (ADR-0025). In load order, lowest priority
/// first: the first copy gives the order, and each later one adds the sections, groups and entries not there yet.
/// An entry is an element without child elements; it is already there when one with the same name and the same
/// attributes is (compared without letter case), so the same hair with another <c>cut</c> is kept. Nothing is removed
/// or reordered, and elements DAORework does not know are kept. A copy that is not well-formed or whose root is not
/// <c>morph_config</c> stops the combining. Pure.
/// </summary>
public static class ChargenMorphCombiner
{
    public const string RootName = "morph_config";

    private const long MaxCharacters = 16 * 1024 * 1024;

    public static Result<CombinedChargenMorph> Combine(IReadOnlyList<ChargenMorphCopy> copies)
    {
        ArgumentNullException.ThrowIfNull(copies);
        if (copies.Count == 0)
        {
            throw new ArgumentException("There is nothing to combine.", nameof(copies));
        }

        XElement? combined = null;
        foreach (var copy in copies)
        {
            if (!Parse(copy).TryGetValue(out var root, out var error))
            {
                return Result.Failure<CombinedChargenMorph>(error);
            }

            if (combined is null)
            {
                combined = new XElement(root);
            }
            else
            {
                AddMissing(combined, root);
            }
        }

        return Result.Success(new CombinedChargenMorph(Write(combined!), combined!.Descendants().Count(IsEntry), [.. copies.Select(c => c.ModId)]));
    }

    private static Result<XElement> Parse(ChargenMorphCopy copy)
    {
        // No DTDs and no external resources: a mod's file can never make DAORework read anything else.
        var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = MaxCharacters };
        try
        {
            using var reader = XmlReader.Create(new StringReader(copy.Xml), settings);
            var root = XDocument.Load(reader).Root;
            if (root is not null && root.Name.LocalName.Equals(RootName, StringComparison.OrdinalIgnoreCase))
            {
                return Result.Success(root);
            }

            return Unreadable(copy, $"its root is <{root?.Name.LocalName}>, not <{RootName}>");
        }
        catch (XmlException ex)
        {
            return Unreadable(copy, ex.Message);
        }
    }

    private static Result<XElement> Unreadable(ChargenMorphCopy copy, string why) =>
        Result.Failure<XElement>(new Error(
            DiagnosticCodes.ModChargenMorphUnreadable,
<<<<<<< HEAD
            $"{GamePaths.ChargenMorphFileName} from {copy.ModName ?? copy.ModId} cannot be read: {why}.",
=======
            $"{GamePaths.ChargenMorphFileName} from {copy.ModId} cannot be read: {why}.",
>>>>>>> origin/main
            "The copies are not combined; the one from the mod last in the load order is used, as without combining."));

    private static void AddMissing(XElement target, XElement source)
    {
        foreach (var child in source.Elements())
        {
            if (IsEntry(child))
            {
                if (!target.Elements().Any(e => IsEntry(e) && SameEntry(e, child)))
                {
                    target.Add(new XElement(child));
                }
            }
            else if (target.Elements().FirstOrDefault(e => !IsEntry(e) && SameName(e, child)) is { } section)
            {
                AddMissing(section, child);
            }
            else
            {
                target.Add(new XElement(child));
            }
        }
    }

    private static bool IsEntry(XElement element) => !element.HasElements;

    private static bool SameName(XElement a, XElement b) => a.Name.LocalName.Equals(b.Name.LocalName, StringComparison.OrdinalIgnoreCase);

    private static bool SameEntry(XElement a, XElement b) =>
        SameName(a, b) && Attributes(a).SetEquals(Attributes(b));

    private static HashSet<string> Attributes(XElement element) =>
        element.Attributes().Select(x => $"{x.Name.LocalName.ToUpperInvariant()}={x.Value.ToUpperInvariant()}").ToHashSet(StringComparer.Ordinal);

    private static string Write(XElement root)
    {
        var settings = new XmlWriterSettings { Indent = true, IndentChars = "  ", NewLineChars = "\r\n", Encoding = new UTF8Encoding(false) };
        var text = new StringBuilder();
        using (var writer = XmlWriter.Create(new Utf8StringWriter(text), settings))
        {
            new XDocument(new XDeclaration("1.0", "utf-8", null), root).Save(writer);
        }

        return text.ToString();
    }

    private sealed class Utf8StringWriter(StringBuilder builder) : StringWriter(builder, System.Globalization.CultureInfo.InvariantCulture)
    {
        public override Encoding Encoding => Encoding.UTF8;
    }
}
