namespace DAORework.FaultInjection.Tests.Gate;

/// <summary>
/// A scenario run once without a kill: how many writes the command makes, and the states before its last setup
/// change (<see cref="Previous"/>), before it, and after it; and the files the scenario's edit changed by hand
/// (<see cref="Edited"/>), as they are in the before-state.
/// </summary>
internal sealed record GateReference(int Writes, IReadOnlyList<string> Previous, IReadOnlyList<string> Before, IReadOnlyList<string> After, IReadOnlyList<string> Edited)
{
    /// <summary>
    /// Gets what a rollback from the before-state returns to: <see cref="Previous"/>, except that a file edited by hand
    /// stays as edited, because a rollback leaves a file changed since it was placed where it is.
    /// </summary>
    public IReadOnlyList<string> RolledBackFromBefore => Edited.Count == 0 ? Previous : FolderState.With(Previous, Edited);
}
