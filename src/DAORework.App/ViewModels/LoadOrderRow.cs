namespace DAORework.App.ViewModels;

/// <summary>One active mod in the load order (P2.5-05): later mods win, so the last one has the highest priority.</summary>
public sealed record LoadOrderRow(string Id, string Name, int Position)
{
    public string Label => $"{Position}. {Name}";
}
