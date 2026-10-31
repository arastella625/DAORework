using System.IO.Abstractions;
using System.Text.Json;
using DAORework.Application.Abstractions;
using DAORework.Application.Common;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Preferences;
using DAORework.Infrastructure.Persistence.Migrations;
using Microsoft.Extensions.Options;

namespace DAORework.Infrastructure.Persistence;

/// <summary><c>preferences.json</c> in the app-data folder (ADR-0017). Loads run the migration chain; saves are atomic.</summary>
public sealed class JsonPreferencesStore : IPreferencesStore
{
    private readonly IFileSystem _fileSystem;
    private readonly DaoReworkOptions _options;

    public JsonPreferencesStore(IFileSystem fileSystem, IOptions<DaoReworkOptions> options)
    {
        ArgumentNullException.ThrowIfNull(fileSystem);
        ArgumentNullException.ThrowIfNull(options);
        _fileSystem = fileSystem;
        _options = options.Value;
    }

    public async Task<Result<AppPreferences>> LoadAsync(CancellationToken cancellationToken = default)
    {
        var path = _options.PreferencesFile;
        if (!_fileSystem.File.Exists(path))
        {
            return Result.Success(new AppPreferences());
        }

        try
        {
            var preferences = await MigratingJsonFile.ReadAsync<AppPreferences>(_fileSystem, path, MigrationChains.Preferences, cancellationToken);
            return Result.Success(preferences ?? new AppPreferences());
        }
        catch (Exception exception) when (exception is InvalidDataException or JsonException or IOException or UnauthorizedAccessException)
        {
            return Result.Failure<AppPreferences>(new Error(
                DiagnosticCodes.PreferencesUnreadable,
                $"The preferences in {path} could not be read: {exception.Message}",
                "Pick a theme again on the Settings page. That replaces the file."));
        }
    }

    public Task SaveAsync(AppPreferences preferences, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        _fileSystem.Directory.CreateDirectory(_options.AppDataDirectory);
        var document = preferences with { SchemaVersion = AppPreferences.CurrentSchemaVersion };
        return AtomicJsonFile.WriteAsync(_fileSystem, _options.PreferencesFile, document, cancellationToken);
    }
}
