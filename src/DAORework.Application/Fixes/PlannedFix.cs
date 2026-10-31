using DAORework.Core.Fixes;

namespace DAORework.Application.Fixes;

/// <summary>A fix plan with what applying it needs: the prepared entry for an apply (null for a remove), and notes to show.</summary>
public sealed record PlannedFix(string FixName, FixPlan Plan, PreparedFix? Prepared)
{
    public IReadOnlyList<string> Notes => Prepared?.Notes ?? [];
}
