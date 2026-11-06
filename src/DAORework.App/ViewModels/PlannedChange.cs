using DAORework.Core.Mods.Planning;

namespace DAORework.App.ViewModels;

/// <summary>What a preview was asked for, kept so it can be planned again without the mods that stop it.</summary>
/// <param name="Kind">Turning on or off.</param>
/// <param name="Mods">The mods still in the change.</param>
/// <param name="RestoredAddIns">Official DLC entries it puts back (ADR-0028 §3).</param>
/// <param name="LeftOut">The mods taken out of it so far, and why.</param>
internal sealed record PlannedChange(
    ModChangeKind Kind,
    IReadOnlyList<LibraryModRow> Mods,
    IReadOnlyList<AddInsListChange> RestoredAddIns,
    IReadOnlyList<LeftOutMod> LeftOut);
