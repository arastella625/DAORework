using System.Text.Json.Nodes;

namespace DAORework.Infrastructure.Persistence.Migrations;

/// <summary>
/// A version step that changes nothing in the document: the newer version only adds optional fields an older document
/// does not have. The version number is still raised, so a build that does not know the new fields refuses the newer
/// document instead of misreading it.
/// </summary>
public sealed class UnchangedMigration(int fromVersion) : IJsonMigration
{
    public int FromVersion => fromVersion;

    public void Apply(JsonObject document, string fileStem)
    {
    }
}
