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
/// <c>morph_config</c> is left out and named in <see cref="CombinedChargenMorph.LeftOut"/>, since the game could not
/// read it either; only when no copy can be read is there nothing to combine. Pure.
/// </summary>
public static class ChargenMorphCombiner
{
    public const string RootName = "morph_config";

    public static Result<CombinedChargenMorph> Combine(IReadOnlyList<ChargenMorphCopy> copies)
    {
        ArgumentNullException.ThrowIfNull(copies);
        if (copies.Count == 0)
        {
            throw new ArgumentException("There is nothing to combine.", nameof(copies));
        }

        XElement? combined = null;
        var used = new List<string>();
        var leftOut = new List<Error>();
        foreach (var copy in copies)
        {
            if (!Parse(copy).TryGetValue(out var root, out var error))
            {
                leftOut.Add(error);
                continue;
            }

            used.Add(copy.ModId);
            if (combined is null)
            {
                combined = new XElement(root);
            }
            else
            {
                AddMissing(combined, root);
            }
        }

        return combined is null
            ? Result.Failure<CombinedChargenMorph>(leftOut[0])
            : Result.Success(new CombinedChargenMorph(Write(combined), combined.Descendants().Count(IsEntry), used) { LeftOut = leftOut });
    }

    private static Result<XElement> Parse(ChargenMorphCopy copy)
    {
        try
        {
            var root = ChargenMorphXml.Load(copy.Xml);
            if (root is not null && root.Name.LocalName.Equals(RootName, StringComparison.OrdinalIgnoreCase))
            {
                return Result.Success(root);
            }

            return Unreadable(copy, $"its root is <{root?.Name.LocalName}>, not <{RootName}>");
        }
        catch (XmlException ex)
        {
            return Unreadable(copy, ex.Message.TrimEnd('.'));
        }
    }

    private static Result<XElement> Unreadable(ChargenMorphCopy copy, string why) =>
        Result.Failure<XElement>(new Error(
            DiagnosticCodes.ModChargenMorphUnreadable,
            $"{GamePaths.ChargenMorphFileName} from {copy.ModName ?? copy.ModId} cannot be read: {why}.",
            "It is left out of the combined file; the game could not have read it either. Ask the mod's author for a fixed file."));

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

    private static bool IsEntry(XElement element) => ChargenMorphXml.IsEntry(element);

    private static bool SameName(XElement a, XElement b) => ChargenMorphXml.SameName(a, b);

    private static bool SameEntry(XElement a, XElement b) => ChargenMorphXml.SameEntry(a, b);

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
