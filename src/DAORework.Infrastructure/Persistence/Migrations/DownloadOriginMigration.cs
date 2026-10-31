using System.Text.Json.Nodes;
using DAORework.Core.Mods.Library;

namespace DAORework.Infrastructure.Persistence.Migrations;

/// <summary>
/// v1 → v2 for <c>mod.json</c> (ADR-0015): adds <c>origin</c>. Before adoption every mod in the library was imported
/// from a download, so every version 1 entry is a <see cref="ModOriginKind.Download"/>.
/// </summary>
public sealed class DownloadOriginMigration : IJsonMigration
{
    public int FromVersion => 1;

    public void Apply(JsonObject document, string fileStem)
    {
        ArgumentNullException.ThrowIfNull(document);
        document["origin"] = new JsonObject { ["kind"] = nameof(ModOriginKind.Download) };
    }
}
