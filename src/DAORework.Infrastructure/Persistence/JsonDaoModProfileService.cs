using System.IO.Abstractions;
using DAORework.Application.Abstractions;
using DAORework.Application.Common;
using DAORework.Core.Mods;
using DAORework.Core.Profiles;
using DAORework.Infrastructure.Persistence.Migrations;
using Microsoft.Extensions.Options;

namespace DAORework.Infrastructure.Persistence;

public sealed class JsonDaoModProfileService : IDaoModProfileService
{
    private readonly IFileSystem _fileSystem;
    private readonly string _modProfilesDirectory;

    public JsonDaoModProfileService(IFileSystem fileSystem, IOptions<DaoReworkOptions> options)
    {
        ArgumentNullException.ThrowIfNull(fileSystem);
        ArgumentNullException.ThrowIfNull(options);
        _fileSystem = fileSystem;
        _modProfilesDirectory = options.Value.ModProfilesDirectory;
        _fileSystem.Directory.CreateDirectory(_modProfilesDirectory);
    }

    public async Task<IReadOnlyList<DaoModProfile>> ListAsync(CancellationToken cancellationToken = default)
    {
        var profiles = new List<DaoModProfile>();
        foreach (var path in _fileSystem.Directory.EnumerateFiles(_modProfilesDirectory, "*.json"))
        {
            var profile = await MigratingJsonFile.ReadAsync<DaoModProfile>(_fileSystem, path, MigrationChains.ModProfiles, cancellationToken);
            if (profile is not null)
            {
                profiles.Add(profile);
            }
        }

        return profiles;
    }

    public Task SaveAsync(DaoModProfile profile, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);
        if (!ProfileName.Create(profile.Name).TryGetValue(out var name, out var error))
        {
            throw new ArgumentException(error.Message, nameof(profile));
        }

        var path = _fileSystem.Path.Combine(_modProfilesDirectory, $"{name.Value}.json");
        var document = profile with { SchemaVersion = DaoModProfile.CurrentSchemaVersion };
        return AtomicJsonFile.WriteAsync(_fileSystem, path, document, cancellationToken);
    }
}
