namespace DAORework.Core.Mods.Handover;

/// <summary>Where the handover from Vortex stands (ADR-0028 §3), worked out from the game folder each time.</summary>
public enum VortexHandoverStep
{
    /// <summary>Vortex manages nothing here, and no handover was started.</summary>
    NothingToDo,

    /// <summary>Vortex still manages mods that have no copy yet.</summary>
    Copy,

    /// <summary>Every mod has a copy, and the player has yet to purge in Vortex.</summary>
    Purge,

    /// <summary>Vortex's files are gone, and the copies are not all on.</summary>
    TurnOn,

    /// <summary>Every copy is on.</summary>
    Done,
}
