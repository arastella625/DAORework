using DAORework.Core.Diagnostics;
using Spectre.Console;

namespace DAORework.Cli.Rendering;

public sealed class DiagnosticReportRenderer
{
    private readonly IAnsiConsole _console;

    public DiagnosticReportRenderer(IAnsiConsole console)
    {
        _console = console;
    }

    public void Render(DiagnosticReport report, string reportPath)
    {
        ArgumentNullException.ThrowIfNull(report);
        _console.WriteLine($"DAO Diagnostic Report: {report.ProfileName}");
        _console.WriteLine();

        foreach (var issue in report.Issues)
        {
            _console.WriteLine($"[{Label(issue.Severity)}] {issue.Code}: {issue.Message}");
            if (!string.IsNullOrWhiteSpace(issue.Recommendation))
            {
                _console.WriteLine($"      Recommendation: {issue.Recommendation}");
            }
        }

        _console.WriteLine();
        _console.WriteLine($"Report written to: {reportPath}");
    }

    private static string Label(DiagnosticSeverity severity) => severity switch
    {
        DiagnosticSeverity.Info => "OK",
        DiagnosticSeverity.Warning => "WARN",
        DiagnosticSeverity.Error => "ERROR",
        _ => severity.ToString().ToUpperInvariant(),
    };
}
