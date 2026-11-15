using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;

namespace DAORework.Core.Mods.AddIns;

/// <summary>
/// Reads the add-ins the game loads from the text of <c>Settings\AddIns.xml</c> (P2.5-23): each <c>AddInItem</c> with
/// <c>Enabled="1"</c> and a number for <c>Priority</c>. An entry missing either is skipped, as the game skips it. The game
/// also skipped an entry that had both but little else (#72), and which other field it needs isn't known, so such a
/// hand-written entry is still listed. Pure.
/// </summary>
public static class AddInsList
{
    public static Result<IReadOnlyList<ListedAddIn>> Parse(string xml)
    {
        ArgumentNullException.ThrowIfNull(xml);
        List<XElement> items;
        try
        {
            items = [.. XDocument.Parse(xml).Descendants("AddInItem")];
        }
        catch (XmlException exception)
        {
            return Result.Failure<IReadOnlyList<ListedAddIn>>(new Error(
                DiagnosticCodes.AddInsListUnreadable,
                $"Settings/AddIns.xml isn't valid XML ({exception.Message}), so add-ins aren't compared.",
                "Close the game, then check again. If it stays, the game may need its add-ins list rebuilt."));
        }

        return Result.Success<IReadOnlyList<ListedAddIn>>([.. items.Select(Listed).OfType<ListedAddIn>()]);
    }

    private static ListedAddIn? Listed(XElement item, int position)
    {
        var uid = (string?)item.Attribute("UID");
        var enabled = (string?)item.Attribute("Enabled") == "1";
        if (string.IsNullOrWhiteSpace(uid) || !enabled
            || !int.TryParse((string?)item.Attribute("Priority"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var priority))
        {
            return null;
        }

        var title = (string?)item.Element("Title")?.Attribute("DefaultText") ?? (string?)item.Attribute("Name");
        return new ListedAddIn(
            uid,
            string.IsNullOrWhiteSpace(title) ? uid : title,
            (string?)item.Attribute("ExtendedModuleUID") ?? string.Empty,
            priority,
            position,
            (string?)item.Attribute("BioWare") == "1");
    }
}
