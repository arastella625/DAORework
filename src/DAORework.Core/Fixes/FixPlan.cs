using DAORework.Core.Mods.Planning;

namespace DAORework.Core.Fixes;

/// <summary>
/// Applying or removing one fix, as a <see cref="ModActivationPlan"/> over the install folder so it is backed up,
/// journaled and recorded exactly as a mod change is (ADR-0013). <see cref="ChangedSince"/> lists recorded files that
/// were changed outside DAORework, such as by Steam's verify: they are left in place and dropped from the record.
/// </summary>
public sealed record FixPlan(FixChangeKind Kind, string EntryId, ModActivationPlan Change, IReadOnlyList<string> ChangedSince);
