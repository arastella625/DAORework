using System.IO.Abstractions;
using System.Text.Json;
using System.Text.Json.Nodes;
using DAORework.Infrastructure.Persistence.Migrations;

namespace DAORework.Infrastructure.Persistence;

/// <summary>Reads a persisted document, upgrades it in memory through its migration chain, then binds it (SDD §8.4).</summary>
public static class MigratingJsonFile
{
    private static readonly JsonNodeOptions _nodeOptions = new() { PropertyNameCaseInsensitive = true };

    /// <exception cref="InvalidDataException">The file is not a JSON object or is newer than this build understands.</exception>
    public static async Task<T?> ReadAsync<T>(IFileSystem fileSystem, string path, JsonMigrationChain chain, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fileSystem);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(chain);

        var document = await ParseObjectAsync(fileSystem, path, cancellationToken);
        chain.Upgrade(document, fileSystem.Path.GetFileNameWithoutExtension(path));
        return document.Deserialize<T>(JsonDefaults.Options);
    }

    private static async Task<JsonObject> ParseObjectAsync(IFileSystem fileSystem, string path, CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = fileSystem.File.OpenRead(path);
            var node = await JsonNode.ParseAsync(stream, _nodeOptions, cancellationToken: cancellationToken);
            return node as JsonObject
                ?? throw new InvalidDataException($"'{path}' does not contain a JSON object.");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException($"'{path}' is not valid JSON.", exception);
        }
    }
}
