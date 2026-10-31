using System.IO.Abstractions;
using System.Text.RegularExpressions;
using DAORework.Application.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods;
using DAORework.Infrastructure.Persistence;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace DAORework.Infrastructure.Tests.Persistence;

public sealed partial class ReportFileWriterTests : IDisposable
{
    private static readonly DateTimeOffset _now = new(2026, 9, 29, 17, 4, 17, 123, TimeSpan.Zero);

    private readonly TempDirectory _temp = new();
    private readonly ReportFileWriter _writer = new(new FileSystem(), new FakeTimeProvider(_now));

    public void Dispose() => _temp.Dispose();

    [Fact]
    public async Task WriteAsync_NamesTheFileWithMillisecondsAndARandomSuffix()
    {
        var path = await _writer.WriteAsync(_temp.Path, "dao-test", new { Value = 1 });

        Assert.Matches(ReportName(), Path.GetFileName(path));
        Assert.StartsWith("dao-test-20260929-170417-123-", Path.GetFileName(path), StringComparison.Ordinal);
        Assert.True(File.Exists(path));
    }

    [Fact]
    public async Task WriteAsync_TwoReportsInTheSameMillisecond_GetDifferentFiles()
    {
        var first = await _writer.WriteAsync(_temp.Path, "dao-test", new { Value = 1 });
        var second = await _writer.WriteAsync(_temp.Path, "dao-test", new { Value = 2 });

        Assert.NotEqual(first, second);
        Assert.Equal(2, Directory.GetFiles(_temp.Path).Length);
    }

    [Fact]
    public async Task DiagnosticReportWriter_WritesAVersionedReportUnderTheReportsDirectory()
    {
        var options = Options.Create(new DaoReworkOptions { AppDataDirectory = _temp.Path });
        var writer = new JsonDiagnosticReportWriter(options, _writer);

        var path = await writer.WriteAsync(new DiagnosticReport { CreatedAtUtc = _now, ProfileName = "P" });

        Assert.Equal(options.Value.ReportsDirectory, Path.GetDirectoryName(path));
        Assert.StartsWith("dao-runtime-report-", Path.GetFileName(path), StringComparison.Ordinal);
        Assert.Contains($"\"schemaVersion\": {DiagnosticReport.CurrentSchemaVersion}", await File.ReadAllTextAsync(path), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(false, "dao-mod-inventory-2")]
    [InlineData(true, "dao-mod-inventory-hashed-2")]
    public async Task ModInventoryWriter_PrefixFollowsTheHashFlag(bool includesHashes, string expectedPrefix)
    {
        var options = Options.Create(new DaoReworkOptions { AppDataDirectory = _temp.Path });
        var writer = new JsonModInventoryWriter(options, _writer);
        var inventory = new ModInventory { CreatedAtUtc = _now, ProfileName = "P", UserDocumentsDirectory = "D", IncludesHashes = includesHashes, Items = [] };

        var path = await writer.WriteAsync(inventory);

        Assert.StartsWith(expectedPrefix, Path.GetFileName(path), StringComparison.Ordinal);
        Assert.Contains($"\"schemaVersion\": {ModInventory.CurrentSchemaVersion}", await File.ReadAllTextAsync(path), StringComparison.Ordinal);
    }

    [GeneratedRegex(@"^dao-test-\d{8}-\d{6}-\d{3}-[0-9a-f]{4}\.json$")]
    private static partial Regex ReportName();
}
