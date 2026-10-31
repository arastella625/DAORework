namespace DAORework.FaultInjection.Tests.Gate;

/// <summary>
/// A scenario run once without a kill: how many writes the command makes, and the states before its last setup
/// change (<see cref="Previous"/>, what a rollback from the before-state returns to), before it, and after it.
/// </summary>
internal sealed record GateReference(int Writes, IReadOnlyList<string> Previous, IReadOnlyList<string> Before, IReadOnlyList<string> After);
