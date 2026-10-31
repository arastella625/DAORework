using DAORework.Core.Diagnostics;

namespace DAORework.Application.Abstractions;

public interface IDiagnosticReportWriter
{
    Task<string> WriteAsync(DiagnosticReport report, CancellationToken cancellationToken = default);
}
