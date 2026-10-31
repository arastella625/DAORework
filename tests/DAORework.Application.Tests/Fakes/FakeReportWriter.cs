using DAORework.Application.Abstractions;
using DAORework.Core.Diagnostics;

namespace DAORework.Application.Tests.Fakes;

internal sealed class FakeReportWriter : IDiagnosticReportWriter
{
    public List<DiagnosticReport> Written { get; } = [];

    public Task<string> WriteAsync(DiagnosticReport report, CancellationToken cancellationToken = default)
    {
        Written.Add(report);
        return Task.FromResult($"report-{Written.Count}.json");
    }
}
