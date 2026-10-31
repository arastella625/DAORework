using DAORework.Application.Abstractions;
using DAORework.Application.Common;
using DAORework.Core.Diagnostics;
using Microsoft.Extensions.Options;

namespace DAORework.Infrastructure.Persistence;

public sealed class JsonDiagnosticReportWriter : IDiagnosticReportWriter
{
    private const string Prefix = "dao-runtime-report";

    private readonly string _reportsDirectory;
    private readonly ReportFileWriter _files;

    public JsonDiagnosticReportWriter(IOptions<DaoReworkOptions> options, ReportFileWriter files)
    {
        _reportsDirectory = options.Value.ReportsDirectory;
        _files = files;
    }

    public Task<string> WriteAsync(DiagnosticReport report, CancellationToken cancellationToken = default) =>
        _files.WriteAsync(_reportsDirectory, Prefix, report, cancellationToken);
}
