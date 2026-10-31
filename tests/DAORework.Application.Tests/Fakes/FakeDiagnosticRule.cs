using DAORework.Application.Diagnostics;
using DAORework.Core.Diagnostics;
using DAORework.Core.Profiles;

namespace DAORework.Application.Tests.Fakes;

internal sealed class FakeDiagnosticRule : IDiagnosticRule
{
    public FakeDiagnosticRule(params DiagnosticIssue[] issues)
    {
        Issues = issues;
    }

    public IReadOnlyList<DiagnosticIssue> Issues { get; }

    public int Calls { get; private set; }

    public Task<IReadOnlyList<DiagnosticIssue>> EvaluateAsync(RuntimeProfile profile, CancellationToken cancellationToken = default)
    {
        Calls++;
        return Task.FromResult(Issues);
    }
}
