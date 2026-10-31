using System.IO.Abstractions;
using System.IO.Compression;
using DAORework.Application.Common;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods;
using DAORework.Core.Profiles;
using DAORework.Core.Support;
using DAORework.Infrastructure.Support;
using DAORework.Infrastructure.Tests.Persistence;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace DAORework.Infrastructure.Tests.Support;

public sealed class ZipSupportBundleWriterTests : IDisposable
{
    private const string UserDocuments = @"C:\Users\player\Documents\BioWare\Dragon Age";

    private static readonly DateTimeOffset _now = new(2026, 9, 29, 20, 0, 0, TimeSpan.Zero);

    private readonly TempDirectory _temp = new();
    private readonly DaoReworkOptions _options;

    public ZipSupportBundleWriterTests()
    {
        _options = new DaoReworkOptions { AppDataDirectory = _temp.Path };
    }

    public void Dispose() => _temp.Dispose();

    [Fact]
    public async Task WriteAsync_WritesEveryEntryRedactedWithNoTempFileLeft()
    {
        var path = await CreateWriter().WriteAsync(Bundle(new ModInventory
        {
            CreatedAtUtc = _now,
            ProfileName = "Default",
            UserDocumentsDirectory = UserDocuments,
            IncludesHashes = false,
            Items = [],
        }));

        Assert.StartsWith("dao-support-bundle-20260929-200000-000-", Path.GetFileName(path), StringComparison.Ordinal);
        Assert.Equal([Path.GetFileName(path)], Directory.GetFiles(_options.ReportsDirectory).Select(Path.GetFileName));
        var entries = ReadEntries(path);
        Assert.Equal(["diagnostics.json", "manifest.json", "mod-inventory.json", "profile.json"], entries.Keys.Order());
        Assert.All(entries.Values, text => Assert.DoesNotContain("player", text, StringComparison.OrdinalIgnoreCase));
        Assert.Contains(@"Users\\<user>\\Documents", entries["profile.json"], StringComparison.Ordinal);
        Assert.Contains($"\"schemaVersion\": {SupportBundle.CurrentSchemaVersion}", entries["manifest.json"], StringComparison.Ordinal);
    }

    [Fact]
    public async Task WriteAsync_FailedInventory_WritesTheReasonInstead()
    {
        var path = await CreateWriter().WriteAsync(Bundle(inventory: null, new Error(DiagnosticCodes.ModFolderUnreadable, $@"Cannot read {UserDocuments}")));

        var entries = ReadEntries(path);
        Assert.False(entries.ContainsKey("mod-inventory.json"));
        Assert.Equal($@"{DiagnosticCodes.ModFolderUnreadable}: Cannot read C:\Users\<user>\Documents\BioWare\Dragon Age", entries["mod-inventory-error.txt"]);
    }

    [Fact]
    public async Task WriteAsync_IncludesRecentLogsOnlyRedactedAndReadableWhileOpen()
    {
        Directory.CreateDirectory(_options.LogsDirectory);
        var recent = Path.Combine(_options.LogsDirectory, "daorework-20260929.log");
        var old = Path.Combine(_options.LogsDirectory, "daorework-20260901.log");
        await File.WriteAllTextAsync(recent, $@"launch from {UserDocuments} as player");
        await File.WriteAllTextAsync(old, "old");
        File.SetLastWriteTimeUtc(recent, _now.UtcDateTime.AddHours(-1));
        File.SetLastWriteTimeUtc(old, _now.UtcDateTime.AddDays(-20));

        string path;
        await using (new FileStream(recent, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
        {
            path = await CreateWriter().WriteAsync(Bundle(inventory: null, new Error("X", "y")));
        }

        var entries = ReadEntries(path);
        Assert.Equal(@"launch from C:\Users\<user>\Documents\BioWare\Dragon Age as <user>", entries["logs/daorework-20260929.log"]);
        Assert.False(entries.ContainsKey("logs/daorework-20260901.log"));
    }

    private static SupportBundle Bundle(ModInventory? inventory, Error? inventoryError = null) => new(
        new RuntimeProfile { Name = "Default", UserDocumentsDirectory = UserDocuments },
        new DiagnosticReport { CreatedAtUtc = _now, ProfileName = "Default", Issues = [new DiagnosticIssue(DiagnosticSeverity.Info, DiagnosticCodes.ProfileValid, "ok")] },
        inventory,
        inventoryError,
        PathRedactor.ForUserProfile(@"C:\Users\player"));

    private static Dictionary<string, string> ReadEntries(string zipPath)
    {
        using var zip = ZipFile.OpenRead(zipPath);
        return zip.Entries.ToDictionary(e => e.FullName, e =>
        {
            using var reader = new StreamReader(e.Open());
            return reader.ReadToEnd();
        });
    }

    private ZipSupportBundleWriter CreateWriter() => new(new FileSystem(), Options.Create(_options), new FakeTimeProvider(_now));
}
