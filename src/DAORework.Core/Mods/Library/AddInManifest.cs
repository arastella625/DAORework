using System.Xml;
using System.Xml.Linq;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;

namespace DAORework.Core.Mods.Library;

/// <summary>What DAORework takes from a <c>.dazip</c>'s <c>Manifest.xml</c>: the add-in's UID and a name to show.</summary>
public sealed record AddInManifest(string Uid, string Name)
{
    public static Result<AddInManifest> Parse(string xml)
    {
        XElement? item;
        try
        {
            item = XDocument.Parse(xml).Descendants("AddInItem").FirstOrDefault();
        }
        catch (XmlException exception)
        {
            return Invalid($"its Manifest.xml is not valid XML ({exception.Message}).");
        }

        var uid = (string?)item?.Attribute("UID");
        if (item is null || string.IsNullOrWhiteSpace(uid))
        {
            return Invalid("its Manifest.xml has no AddInItem with a UID.");
        }

        // The title is either an attribute of Title or of a LocalizedString inside it.
        var title = item.Element("Title");
        var name = (string?)title?.Attribute("DefaultText")
            ?? (string?)title?.Element("LocalizedString")?.Attribute("DefaultText")
            ?? (string?)item.Attribute("Name");
        return Result.Success(new AddInManifest(uid, string.IsNullOrWhiteSpace(name) ? uid : name));
    }

    private static Result<AddInManifest> Invalid(string reason) =>
        Result.Failure<AddInManifest>(new Error(DiagnosticCodes.ModImportInvalid, $"This is not a usable .dazip: {reason}"));
}
