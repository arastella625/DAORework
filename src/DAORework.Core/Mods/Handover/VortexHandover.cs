namespace DAORework.Core.Mods.Handover;

/// <summary>
/// What the handover from Vortex keeps (ADR-0028 §3), since the purge deletes Vortex's own record: the copies, in the
/// order that makes the same files win, and every file Vortex had placed, to tell when the purge is done.
/// </summary>
public sealed record VortexHandover(IReadOnlyList<VortexCopy> Copies, IReadOnlyList<string> PlacedPaths)
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;

    /// <summary>Gets the Vortex mods the handover can't take, since they run inside the game (ADR-0033): nothing more is asked of them.</summary>
    public IReadOnlyList<string> LeftOut { get; init; } = [];
}
