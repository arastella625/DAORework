using DAORework.Core.Common;

namespace DAORework.Application.Fixes;

/// <summary>
/// One fix DAORework can apply (ADR-0013, SDD §5 Strategy). It works out the library entry to apply from the install
/// folder or the file the user supplies; planning, backup and applying are the same for every fix.
/// </summary>
public interface IManagedFix
{
    /// <summary>Gets the fix's id, as in the known-items catalog.</summary>
    string Id { get; }

    /// <summary>Gets the other names the CLI accepts for it, such as "laa".</summary>
    IReadOnlyList<string> Aliases { get; }

    Task<Result<PreparedFix>> PrepareAsync(FixApplyContext context, CancellationToken cancellationToken = default);
}
