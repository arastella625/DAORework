namespace DAORework.App.ViewModels;

/// <summary>Which kind of mod the Mods tab shows (ADR-0026): every kind, or one of the three cards.</summary>
public enum ModKindFilter
{
    All,
    AddIns,
    OverrideMods,
    CombinedFiles,
}

/// <summary>How the Mods tab's list is sorted (P2.6-30), in the order the box offers them.</summary>
public enum ModSort
{
    Name,
    Newest,
    OnFirst,
}

/// <summary>Which mods the Mods tab's list shows: all, those on, those off, or those found in the game.</summary>
public enum ModShowFilter
{
    All,
    On,
    Off,
    Found,
    GameFolder,
}
