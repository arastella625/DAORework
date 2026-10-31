using DAORework.Core.Fixes;

namespace DAORework.Application.Fixes;

/// <summary>
/// The entry to apply, and its file contents when it is not in the library yet (stored only when the plan is
/// applied, so a dry run writes nothing); the hashes of the files it is made to replace; and anything to tell the user.
/// </summary>
public sealed record PreparedFix(FixEntry Entry, IReadOnlyDictionary<string, byte[]> NewContents, IReadOnlySet<string> Replaceable, IReadOnlyList<string> Notes)
{
    /// <summary>Gets entries to store alongside, holding files the fix replaces that are not in the library yet.</summary>
    public IReadOnlyList<KeptEntry> AlsoKeep { get; init; } = [];
}
