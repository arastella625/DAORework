using DAORework.Application.Abstractions;
using DAORework.Application.Common;
using DAORework.Core.Mods;
using Microsoft.Extensions.Options;

namespace DAORework.Infrastructure.Persistence;

public sealed class JsonModInventoryWriter : IModInventoryWriter
{
    private const string Prefix = "dao-mod-inventory";
    private const string HashedPrefix = "dao-mod-inventory-hashed";

    private readonly string _reportsDirectory;
    private readonly ReportFileWriter _files;

    public JsonModInventoryWriter(IOptions<DaoReworkOptions> options, ReportFileWriter files)
    {
        _reportsDirectory = options.Value.ReportsDirectory;
        _files = files;
    }

    public Task<string> WriteAsync(ModInventory report, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(report);
        var prefix = report.IncludesHashes ? HashedPrefix : Prefix;
        return _files.WriteAsync(_reportsDirectory, prefix, report, cancellationToken);
    }
}
