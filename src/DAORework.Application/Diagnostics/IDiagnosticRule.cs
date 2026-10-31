using DAORework.Core.Diagnostics;
using DAORework.Core.Profiles;

namespace DAORework.Application.Diagnostics;

/// <summary>One diagnostic check (Strategy, SDD §5). New checks are new classes, registered in order.</summary>
public interface IDiagnosticRule
{
    Task<IReadOnlyList<DiagnosticIssue>> EvaluateAsync(RuntimeProfile profile, CancellationToken cancellationToken = default);
}
