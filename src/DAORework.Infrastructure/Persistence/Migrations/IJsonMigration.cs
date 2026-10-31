using System.Text.Json.Nodes;

namespace DAORework.Infrastructure.Persistence.Migrations;

/// <summary>Upgrades a persisted document from <see cref="FromVersion"/> to <c>FromVersion + 1</c> in place (SDD §8.4).</summary>
public interface IJsonMigration
{
    int FromVersion { get; }

    /// <param name="document">The document to change in place. The chain stamps the new version afterwards.</param>
    /// <param name="fileStem">The file name without extension, which is the key the document was stored under.</param>
    void Apply(JsonObject document, string fileStem);
}
