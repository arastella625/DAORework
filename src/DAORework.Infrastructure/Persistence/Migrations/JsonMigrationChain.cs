using System.Text.Json.Nodes;

namespace DAORework.Infrastructure.Persistence.Migrations;

/// <summary>
/// Runs migrators <c>n → n+1</c> in sequence until a document is at the current schema version (SDD §8.4).
/// A document without <c>schemaVersion</c> is version 0. A document newer than this build is refused.
/// </summary>
public sealed class JsonMigrationChain
{
    public const string SchemaVersionProperty = "schemaVersion";

    private readonly Dictionary<int, IJsonMigration> _migrations;

    public JsonMigrationChain(int currentVersion, params IJsonMigration[] migrations)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(currentVersion);
        ArgumentNullException.ThrowIfNull(migrations);

        _migrations = migrations.ToDictionary(m => m.FromVersion);
        for (var version = 0; version < currentVersion; version++)
        {
            if (!_migrations.ContainsKey(version))
            {
                throw new ArgumentException($"No migration from schema version {version} to {version + 1}.", nameof(migrations));
            }
        }

        CurrentVersion = currentVersion;
    }

    public int CurrentVersion { get; }

    /// <summary>Upgrades <paramref name="document"/> in place. Returns true when anything changed.</summary>
    /// <exception cref="InvalidDataException">The document is from a newer schema than this build understands.</exception>
    public bool Upgrade(JsonObject document, string fileStem)
    {
        ArgumentNullException.ThrowIfNull(document);

        var version = ReadVersion(document);
        if (version > CurrentVersion)
        {
            throw new InvalidDataException(
                $"'{fileStem}' has schema version {version}, but this build understands up to {CurrentVersion}. Update DAORework.");
        }

        var changed = false;
        while (version < CurrentVersion)
        {
            _migrations[version].Apply(document, fileStem);
            version++;
            document[SchemaVersionProperty] = version;
            changed = true;
        }

        return changed;
    }

    private static int ReadVersion(JsonObject document) =>
        document.TryGetPropertyValue(SchemaVersionProperty, out var node) && node is not null
            ? node.GetValue<int>()
            : 0;
}
