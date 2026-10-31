using System.Text.Json.Nodes;

namespace DAORework.Infrastructure.Persistence.Migrations;

/// <summary>
/// v0 → v1 for profile documents (audit C7). Version 0 sanitized the name on save without reversing it on list,
/// so <c>My:Profile</c> was stored in <c>My_Profile.json</c> under a name that can no longer be loaded. The file
/// stem is the key every lookup uses, so the stored name becomes the stem.
/// </summary>
public sealed class NameFromFileStemMigration : IJsonMigration
{
    public int FromVersion => 0;

    public void Apply(JsonObject document, string fileStem)
    {
        ArgumentNullException.ThrowIfNull(document);
        document["name"] = fileStem;
    }
}
