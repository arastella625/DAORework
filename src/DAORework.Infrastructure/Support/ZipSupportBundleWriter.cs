using System.Globalization;
using System.IO.Abstractions;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DAORework.Application.Abstractions;
using DAORework.Application.Common;
using DAORework.Core.Support;
using DAORework.Infrastructure.Persistence;
using Microsoft.Extensions.Options;

namespace DAORework.Infrastructure.Support;

/// <summary>
/// Writes <c>dao-support-bundle-{timestamp}-{random}.zip</c> into the reports folder: a manifest, the profile, the
/// diagnostics, the inventory (or why it failed) and the newest logs, every entry passed through the redactor.
/// Built in a temp file and moved into place, like every other write (SDD §8.4).
/// </summary>
public sealed class ZipSupportBundleWriter : ISupportBundleWriter
{
    public const int MaxLogFiles = 5;
    public const int MaxLogBytes = 1024 * 1024;
    public static readonly TimeSpan LogWindow = TimeSpan.FromDays(7);

    private readonly IFileSystem _fileSystem;
    private readonly DaoReworkOptions _options;
    private readonly TimeProvider _clock;

    public ZipSupportBundleWriter(IFileSystem fileSystem, IOptions<DaoReworkOptions> options, TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(options);
        _fileSystem = fileSystem;
        _options = options.Value;
        _clock = clock;
    }

    public async Task<string> WriteAsync(SupportBundle bundle, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bundle);
        _fileSystem.Directory.CreateDirectory(_options.ReportsDirectory);

        var now = _clock.GetUtcNow();
        var name = $"dao-support-bundle-{now.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture)}-{RandomNumberGenerator.GetHexString(4, lowercase: true)}.zip";
        var path = _fileSystem.Path.Combine(_options.ReportsDirectory, name);
        var tempPath = path + ".tmp";

        try
        {
            await using (var stream = _fileSystem.FileStream.New(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                using var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true);
                await AddJsonAsync(zip, "manifest.json", Manifest(bundle, now), bundle.Redactor, cancellationToken);
                await AddJsonAsync(zip, "profile.json", bundle.Profile, bundle.Redactor, cancellationToken);
                await AddJsonAsync(zip, "diagnostics.json", bundle.Diagnostics, bundle.Redactor, cancellationToken);
                await AddInventoryAsync(zip, bundle, cancellationToken);
                await AddLogsAsync(zip, bundle.Redactor, now, cancellationToken);
            }

            _fileSystem.File.Move(tempPath, path, overwrite: false);
            return path;
        }
        finally
        {
            if (_fileSystem.File.Exists(tempPath))
            {
                _fileSystem.File.Delete(tempPath);
            }
        }
    }

    private static object Manifest(SupportBundle bundle, DateTimeOffset now) => new
    {
        SchemaVersion = SupportBundle.CurrentSchemaVersion,
        CreatedAtUtc = now,
        ProfileName = bundle.Profile.Name,
        AppVersion = typeof(ZipSupportBundleWriter).Assembly.GetName().Version?.ToString(),
        OperatingSystem = RuntimeInformation.OSDescription,
        Runtime = RuntimeInformation.FrameworkDescription,
        Redaction = $"The folder after Users\\ and the account name are replaced with {PathRedactor.Placeholder}.",
    };

    private static Task AddInventoryAsync(ZipArchive zip, SupportBundle bundle, CancellationToken cancellationToken) =>
        bundle.Inventory is not null
            ? AddJsonAsync(zip, "mod-inventory.json", bundle.Inventory, bundle.Redactor, cancellationToken)
            : AddTextAsync(zip, "mod-inventory-error.txt", $"{bundle.InventoryError?.Code}: {bundle.InventoryError?.Message}", bundle.Redactor, cancellationToken);

    private static Task AddJsonAsync(ZipArchive zip, string entryName, object value, PathRedactor redactor, CancellationToken cancellationToken) =>
        AddTextAsync(zip, entryName, JsonSerializer.Serialize(value, JsonDefaults.Options), redactor, cancellationToken);

    private static async Task AddTextAsync(ZipArchive zip, string entryName, string text, PathRedactor redactor, CancellationToken cancellationToken)
    {
        var entry = zip.CreateEntry(entryName, CompressionLevel.Optimal);
        await using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        await writer.WriteAsync(redactor.Redact(text).AsMemory(), cancellationToken);
    }

    private async Task AddLogsAsync(ZipArchive zip, PathRedactor redactor, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (!_fileSystem.Directory.Exists(_options.LogsDirectory))
        {
            return;
        }

        var recent = _fileSystem.DirectoryInfo.New(_options.LogsDirectory).EnumerateFiles("*.log")
            .Where(file => file.LastWriteTimeUtc >= (now - LogWindow).UtcDateTime)
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .Take(MaxLogFiles);

        foreach (var file in recent)
        {
            await AddTextAsync(zip, $"logs/{file.Name}", await ReadTailAsync(file.FullName, cancellationToken), redactor, cancellationToken);
        }
    }

    /// <summary>The last <see cref="MaxLogBytes"/> of a log, readable while Serilog still has it open.</summary>
    private async Task<string> ReadTailAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = _fileSystem.FileStream.New(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        if (stream.Length > MaxLogBytes)
        {
            stream.Seek(-MaxLogBytes, SeekOrigin.End);
        }

        using var reader = new StreamReader(stream, Encoding.UTF8);
        return await reader.ReadToEndAsync(cancellationToken);
    }
}
