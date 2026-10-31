using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Apply;
using DAORework.Core.Mods.Backups;
using DAORework.Core.Mods.Planning;
using Xunit;
using static DAORework.Infrastructure.Tests.Mods.ModScenario;

namespace DAORework.Infrastructure.Tests.Mods;

/// <summary>The deployment record and the apply journal, as kept under app-data.</summary>
public sealed class DeploymentStoresTests
{
    private static readonly string _elsewhere = System.IO.Abstractions.TestingHelpers.MockUnixSupport.Path(@"C:\Elsewhere");

    private readonly ModScenario _scenario = new();

    [Fact]
    public async Task Record_RoundTripsAndIsEmptyForAFolderNeverDeployedTo()
    {
        var record = new DeploymentRecord(["a"], [new RecordedFile("x/y.dds", "abc", "a")], [new RecordedAddIn("uid", "a")]);

        await _scenario.Records.SaveAsync(_elsewhere, record);

        var loaded = (await _scenario.Records.LoadAsync(_elsewhere)).Value;
        Assert.Equal(record.ActiveModIds, loaded.ActiveModIds);
        Assert.Equal(record.Files, loaded.Files);
        Assert.Equal(record.AddedAddIns, loaded.AddedAddIns);
        Assert.Same(DeploymentRecord.Empty, (await _scenario.Records.LoadAsync(Documents + "2")).Value);
    }

    [Theory]
    [InlineData("{ not json")]
    [InlineData("null")]
    public async Task Record_Unreadable_IsAFailureNotAnEmptyRecord(string content)
    {
        _scenario.Write(Path.Combine(AppData, "deployments"), DocumentsKey.For(_elsewhere) + ".json", content);

        Assert.Equal(DiagnosticCodes.ModRecordUnreadable, (await _scenario.Records.LoadAsync(_elsewhere)).Error!.Code);
    }

    [Fact]
    public async Task Journal_LogsStepsAndIgnoresALineCutShortByACrash()
    {
        var journal = new ApplyJournal(_elsewhere, "b1", [Op("a"), Op("b"), Op("c")], [], DeploymentRecord.Empty, DeploymentRecord.Empty);
        await _scenario.Journals.StartAsync(journal);
        await _scenario.Journals.MarkDoneAsync(_elsewhere, 0);
        await _scenario.Journals.MarkDoneAsync(_elsewhere, 2);
        await _scenario.FileSystem.File.AppendAllTextAsync(Path.Combine(AppData, "deployments", DocumentsKey.For(_elsewhere) + ".journal.log"), "1x\n9\n");

        var progress = await _scenario.Journals.LoadAsync(_elsewhere);

        Assert.Equal("b1", progress!.Journal.BackupId);
        Assert.Equal(3, progress.Journal.StepCount);
        Assert.Equal([0, 2], progress.DoneSteps.Order());
    }

    [Fact]
    public async Task Journal_StartingAgain_ForgetsAnOldLogAndDeletingEndsIt()
    {
        var journal = new ApplyJournal(_elsewhere, "b1", [Op("a")], [new AddInsListChange("u", "m", Add: true)], DeploymentRecord.Empty, DeploymentRecord.Empty);
        await _scenario.Journals.StartAsync(journal);
        await _scenario.Journals.MarkDoneAsync(_elsewhere, 1);
        await _scenario.Journals.StartAsync(journal);

        Assert.Empty((await _scenario.Journals.LoadAsync(_elsewhere))!.DoneSteps);
        Assert.Equal(2, journal.StepCount);

        await _scenario.Journals.DeleteAsync(_elsewhere);

        Assert.Null(await _scenario.Journals.LoadAsync(_elsewhere));
    }

    private static PlanOperation Op(string name) => new(PlanAction.Delete, $"x/{name}", "m", null, "h");
}
