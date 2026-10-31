using DAORework.Core.Common;
using DAORework.Core.Diagnostics;

namespace DAORework.Application.Diagnostics;

/// <summary>Runs every registered <see cref="IDiagnosticRule"/> in order and aggregates the result (Composite, SDD §5).</summary>
public sealed class RunDiagnostics
{
    private readonly IReadOnlyList<IDiagnosticRule> _rules;
    private readonly TimeProvider _timeProvider;

    public RunDiagnostics(IEnumerable<IDiagnosticRule> rules, TimeProvider timeProvider)
    {
        _rules = rules.ToList();
        _timeProvider = timeProvider;
    }

    public async Task<Result<DiagnosticReport>> ExecuteAsync(RunDiagnosticsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var issues = new List<DiagnosticIssue>();

        foreach (var rule in _rules)
        {
            cancellationToken.ThrowIfCancellationRequested();
            issues.AddRange(await rule.EvaluateAsync(request.Profile, cancellationToken));
        }

        if (!issues.Any(i => i.Severity == DiagnosticSeverity.Error))
        {
            issues.Add(new DiagnosticIssue(
                DiagnosticSeverity.Info,
                DiagnosticCodes.ProfileValid,
                "The runtime profile passed launch validation."));
        }

        return Result.Success(new DiagnosticReport
        {
            CreatedAtUtc = _timeProvider.GetUtcNow(),
            ProfileName = request.Profile.Name,
            Issues = issues,
            ProfileSnapshot = request.Profile,
        });
    }
}
