using System.IO.Abstractions;
using DAORework.Application.Common;
using DAORework.Application.Profiles;
using DAORework.Core.Profiles;
using DAORework.Infrastructure.Persistence.Migrations;
using Microsoft.Extensions.Options;

namespace DAORework.Infrastructure.Persistence;

/// <summary>One <c>{name}.json</c> per profile. Loads run the migration chain in memory; saves are atomic and stamp the current schema.</summary>
public sealed class JsonProfileRepository : IProfileRepository
{
    private readonly IFileSystem _fileSystem;
    private readonly string _profilesDirectory;

    public JsonProfileRepository(IFileSystem fileSystem, IOptions<DaoReworkOptions> options)
    {
        ArgumentNullException.ThrowIfNull(fileSystem);
        ArgumentNullException.ThrowIfNull(options);
        _fileSystem = fileSystem;
        _profilesDirectory = options.Value.ProfilesDirectory;
        _fileSystem.Directory.CreateDirectory(_profilesDirectory);
    }

    public async Task<RuntimeProfile?> LoadAsync(string profileName, CancellationToken cancellationToken = default)
    {
        // An invalid name cannot belong to any stored profile.
        if (!ProfileName.Create(profileName).TryGetValue(out var name, out _))
        {
            return null;
        }

        var path = GetProfilePath(name);
        if (!_fileSystem.File.Exists(path))
        {
            return null;
        }

        return await MigratingJsonFile.ReadAsync<RuntimeProfile>(_fileSystem, path, MigrationChains.RuntimeProfiles, cancellationToken);
    }

    public Task SaveAsync(RuntimeProfile profile, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var path = GetProfilePath(RequireValidName(profile.Name));
        var document = profile with { SchemaVersion = RuntimeProfile.CurrentSchemaVersion };
        return AtomicJsonFile.WriteAsync(_fileSystem, path, document, cancellationToken);
    }

    public Task<IReadOnlyList<string>> ListNamesAsync(CancellationToken cancellationToken = default)
    {
        var names = _fileSystem.Directory.EnumerateFiles(_profilesDirectory, "*.json")
            .Select(_fileSystem.Path.GetFileNameWithoutExtension)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Cast<string>()
            .ToList();

        return Task.FromResult<IReadOnlyList<string>>(names);
    }

    private static ProfileName RequireValidName(string candidate)
    {
        // Callers validate names at the boundary (the use cases); an invalid name here is a programming error.
        return ProfileName.Create(candidate).TryGetValue(out var name, out var error)
            ? name
            : throw new ArgumentException(error.Message, nameof(candidate));
    }

    private string GetProfilePath(ProfileName name) => _fileSystem.Path.Combine(_profilesDirectory, $"{name.Value}.json");
}
