namespace DAORework.Core.Mods.Library;

/// <summary>
/// The DLLs the game ships in <c>bin_ship</c>, read from a real Steam install (ADR-0033 §6). GOG and EA copies are
/// compared with this list before P2.8 ships.
/// </summary>
public static class GameOwnDlls
{
    private static readonly HashSet<string> _names = new(StringComparer.OrdinalIgnoreCase)
    {
        "binkw32.dll", "DAOriginsGDF.dll", "DAUpdater.Engine.dll", "fmod_event.dll", "fmodex.dll", "icsharpcode.sharpziplib.dll",
        "msvcp71.dll", "msvcr71.dll", "nxcharacter.dll", "nxcooking.dll", "speedtreert.dll", "umbra.dll", "unrar.dll",
    };

    public static bool Contains(string fileName) => _names.Contains(fileName);
}
