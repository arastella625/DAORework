using System.Text.Json.Nodes;

namespace DAORework.Infrastructure.Persistence.Migrations;

/// <summary>
/// v0 → v1 for <c>mod.json</c>. Every <c>mod.json</c> has carried <c>schemaVersion</c> since the library was added
/// (P2-07), so a document without it is a version 1 document that lost the field. Nothing else changes.
/// </summary>
public sealed class UnversionedLibraryEntryMigration : IJsonMigration
{
    public int FromVersion => 0;

    public void Apply(JsonObject document, string fileStem)
    {
    }
}
