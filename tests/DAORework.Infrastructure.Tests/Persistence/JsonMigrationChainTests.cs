using System.Text.Json.Nodes;
using DAORework.Infrastructure.Persistence.Migrations;
using Xunit;

namespace DAORework.Infrastructure.Tests.Persistence;

public sealed class JsonMigrationChainTests
{
    [Fact]
    public void Upgrade_DocumentWithoutVersion_RunsEveryStepAndStampsTheCurrentVersion()
    {
        var chain = new JsonMigrationChain(2, new AppendStep(0), new AppendStep(1));
        var document = new JsonObject { ["name"] = "x" };

        var changed = chain.Upgrade(document, "stem");

        Assert.True(changed);
        Assert.Equal(2, (int)document[JsonMigrationChain.SchemaVersionProperty]!);
        Assert.Equal("0:stem,1:stem", (string)document["trail"]!);
    }

    [Fact]
    public void Upgrade_CurrentDocument_IsUntouched()
    {
        var chain = new JsonMigrationChain(1, new AppendStep(0));
        var document = new JsonObject { [JsonMigrationChain.SchemaVersionProperty] = 1 };

        var changed = chain.Upgrade(document, "stem");

        Assert.False(changed);
        Assert.False(document.ContainsKey("trail"));
    }

    [Fact]
    public void Upgrade_NewerDocument_IsRefused()
    {
        var chain = new JsonMigrationChain(1, new AppendStep(0));
        var document = new JsonObject { [JsonMigrationChain.SchemaVersionProperty] = 2 };

        var exception = Assert.Throws<InvalidDataException>(() => chain.Upgrade(document, "stem"));

        Assert.Contains("schema version 2", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Constructor_MissingStep_Throws()
    {
        Assert.Throws<ArgumentException>(() => new JsonMigrationChain(2, new AppendStep(1)));
    }

    private sealed class AppendStep(int fromVersion) : IJsonMigration
    {
        public int FromVersion => fromVersion;

        public void Apply(JsonObject document, string fileStem)
        {
            var trail = document["trail"]?.GetValue<string>();
            document["trail"] = trail is null ? $"{fromVersion}:{fileStem}" : $"{trail},{fromVersion}:{fileStem}";
        }
    }
}
