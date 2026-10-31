using System.Globalization;
using System.IO.Abstractions;
using System.Security.Cryptography;

namespace DAORework.Infrastructure.Persistence;

/// <summary>
/// Writes a report under a name that cannot collide (SDD §8.4, audit C6):
/// <c>{prefix}-yyyyMMdd-HHmmss-fff-{random}.json</c>, created without overwriting. If the name is taken
/// anyway, a new suffix is drawn.
/// </summary>
public sealed class ReportFileWriter
{
    private const int MaxAttempts = 16;

    private readonly IFileSystem _fileSystem;
    private readonly TimeProvider _clock;

    public ReportFileWriter(IFileSystem fileSystem, TimeProvider clock)
    {
        _fileSystem = fileSystem;
        _clock = clock;
    }

    public async Task<string> WriteAsync<T>(string directory, string prefix, T report, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);

        _fileSystem.Directory.CreateDirectory(directory);
        var timestamp = _clock.GetUtcNow().ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture);

        for (var attempt = 1; ; attempt++)
        {
            var path = _fileSystem.Path.Combine(directory, $"{prefix}-{timestamp}-{RandomSuffix()}.json");
            try
            {
                await AtomicJsonFile.WriteAsync(_fileSystem, path, report, overwrite: false, cancellationToken);
                return path;
            }
            catch (IOException) when (attempt < MaxAttempts && _fileSystem.File.Exists(path))
            {
                // Another report took this name in the same millisecond; draw a new suffix.
            }
        }
    }

    private static string RandomSuffix() => RandomNumberGenerator.GetHexString(4, lowercase: true);
}
