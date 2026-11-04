namespace DAORework.App.ViewModels;

/// <summary>Which kind of mod the Mods tab shows (ADR-0026): every kind, or one of the three cards.</summary>
public enum ModKindFilter
{
    All,
    AddIns,
    OverrideMods,
    CombinedFiles,
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
