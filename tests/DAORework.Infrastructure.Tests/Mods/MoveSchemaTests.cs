using System.Text.Json.Nodes;
using DAORework.Core.Mods.Apply;
using DAORework.Core.Mods.Backups;
using Xunit;
using static DAORework.Infrastructure.Tests.Mods.ModScenario;

namespace DAORework.Infrastructure.Tests.Mods;

/// <summary>
/// Backup manifests and apply journals are schema 2 with moves (ADR-0015): one written before still reads, and one from
/// a newer build is refused rather than misread, since a build that does not know moves would delete a moved file.
/// </summary>
public sealed class MoveSchemaTests
{
    private readonly ModScenario _scenario = new();

    [Fact]
    public async Task Backups_VersionOneManifestStillListsAndANewerOneIsRefused()
    {
        await _scenario.Apply(_scenario.Writer).ExecuteAsync(new(Documents, await _scenario.PlanAsync()));
        var manifest = Assert.Single(_scenario.FileSystem.Directory.EnumerateFiles(Path.Combine(AppData, "deployment-backups"), "manifest.json", SearchOption.AllDirectories));

        Rewrite(manifest, 1);
        var listed = Assert.Single(await _scenario.Backups.ListAsync(Documents));
        Rewrite(manifest, DeploymentBackup.CurrentSchemaVersion + 1);

        Assert.Equal(DeploymentBackup.CurrentSchemaVersion, listed.SchemaVersion);
        await Assert.ThrowsAsync<InvalidDataException>(() => _scenario.Backups.ListAsync(Documents));
    }

    [Fact]
    public async Task Journal_FromANewerBuild_IsRefused()
    {
        var journal = new ApplyJournal(Documents, "backup", [], [], (await _scenario.Records.LoadAsync(Documents)).Value, (await _scenario.Records.LoadAsync(Documents)).Value);
        await _scenario.Journals.StartAsync(journal);
        var file = Assert.Single(_scenario.FileSystem.Directory.EnumerateFiles(Path.Combine(AppData, "deployments"), "*.journal.json"));

        Rewrite(file, 1);
        var progress = await _scenario.Journals.LoadAsync(Documents);
        Rewrite(file, ApplyJournal.CurrentSchemaVersion + 1);

        Assert.Equal(ApplyJournal.CurrentSchemaVersion, progress!.Journal.SchemaVersion);
        await Assert.ThrowsAsync<InvalidDataException>(() => _scenario.Journals.LoadAsync(Documents));
    }

    private void Rewrite(string path, int version)
    {
        var json = JsonNode.Parse(_scenario.FileSystem.File.ReadAllText(path))!.AsObject();
        json["schemaVersion"] = version;
        _scenario.FileSystem.File.WriteAllText(path, json.ToJsonString());
    }
}
