namespace DAORework.Core.Mods.AddIns;

/// <summary>Two or more add-ins give the game a file of the same name; it uses <paramref name="Winner"/>'s copy.</summary>
public sealed record AddInClash(string FileName, ListedAddIn Winner, IReadOnlyList<ListedAddIn> Others)
{
    /// <summary>Gets a value indicating whether the winner won only by coming first in <c>AddIns.xml</c>, on an equal priority.</summary>
    public bool WonOnPosition => Others.Any(o => o.Priority == Winner.Priority);
}
