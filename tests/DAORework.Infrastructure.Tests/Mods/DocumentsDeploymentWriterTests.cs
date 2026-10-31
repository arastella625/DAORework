using System.Text;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Library;
using DAORework.Core.Mods.Planning;
using Xunit;
using static DAORework.Infrastructure.Tests.Mods.ModScenario;

namespace DAORework.Infrastructure.Tests.Mods;

public sealed class DocumentsDeploymentWriterTests
{
    private const string Hair = "packages/core/override/DAORework/hair/textures/hair.dds";

    private readonly ModScenario _scenario = new();

    [Fact]
    public async Task Copy_TwiceInARow_WritesOnceAndSucceedsBothTimes()
    {
        Assert.True((await Apply(Copy())).IsSuccess);
        Assert.True((await Apply(Copy())).IsSuccess);

        Assert.Equal("hair", Read(Hair));
        Assert.False(_scenario.FileSystem.File.Exists(Full(Documents, Hair) + ".daorework-apply"));
    }

    [Fact]
    public async Task Copy_OverSomethingElse_IsRefusedAndLeavesItAlone()
    {
        _scenario.Write(Documents, Hair, "not ours");

        var result = await Apply(Copy());

        Assert.Equal(DiagnosticCodes.ModApplyFailed, result.Error!.Code);
        Assert.Equal("not ours", Read(Hair));
    }

    [Fact]
    public async Task Copy_OverAStaleTemporaryFile_Succeeds()
    {
        _scenario.Write(Documents, Hair + ".daorework-apply", "half a fi");

        Assert.True((await Apply(Copy())).IsSuccess);
        Assert.Equal("hair", Read(Hair));
    }

    [Fact]
    public async Task Copy_LibraryFileMissing_Fails()
    {
        _scenario.FileSystem.File.Delete(Path.Combine(AppData, "library", "hair", "textures", "hair.dds"));

        Assert.Contains("is missing", (await Apply(Copy())).Error!.Message, StringComparison.Ordinal);
        Assert.False(_scenario.FileSystem.File.Exists(Full(Documents, Hair)));
    }

    [Fact]
    public async Task Replace_OnlyFromTheExpectedContent()
    {
        _scenario.Write(Documents, Hair, "hair v0");

        var refused = await Apply(Copy() with { Action = PlanAction.Replace, ExpectedSha256 = Sha("something else") });
        var replaced = await Apply(Copy() with { Action = PlanAction.Replace, ExpectedSha256 = Sha("hair v0") });

        Assert.True(refused.IsFailure);
        Assert.True(replaced.IsSuccess);
        Assert.Equal("hair", Read(Hair));
    }

    [Fact]
    public async Task Delete_RemovesTheFileAndItsEmptyOwnedFoldersButNeverTheGameFolders()
    {
        _scenario.Write(Documents, Hair, "hair");
        _scenario.FileSystem.File.Delete(Full(Documents, "packages/core/override/DAORework/ui/ui.gda"));
        _scenario.FileSystem.File.Delete(Full(Documents, "packages/core/override/DAORework/old/old.gda"));
        _scenario.FileSystem.File.Delete(Full(Documents, "packages/core/override/My Mods/mine.dds"));
        _scenario.FileSystem.Directory.Delete(Full(Documents, "packages/core/override/My Mods"));

        var result = await Apply(new PlanOperation(PlanAction.Delete, Hair, "hair", null, Sha("hair")));

        Assert.True(result.IsSuccess);
        Assert.False(_scenario.FileSystem.Directory.Exists(Full(Documents, "packages/core/override/DAORework/hair")));
        Assert.True(_scenario.FileSystem.Directory.Exists(Full(Documents, "packages/core/override")));
    }

    [Fact]
    public async Task Delete_ChangedFile_IsRefusedAndAlreadyGoneIsFine()
    {
        _scenario.Write(Documents, Hair, "edited");

        var refused = await Apply(new PlanOperation(PlanAction.Delete, Hair, "hair", null, Sha("hair")));
        _scenario.FileSystem.File.Delete(Full(Documents, Hair));
        var gone = await Apply(new PlanOperation(PlanAction.Delete, Hair, "hair", null, Sha("hair")));

        Assert.True(refused.IsFailure);
        Assert.True(gone.IsSuccess);
    }

    [Fact]
    public async Task Apply_LockedTarget_FailsWithoutThrowing()
    {
        _scenario.FileSystem.AddFile(Full(Documents, Hair), new System.IO.Abstractions.TestingHelpers.MockFileData("x") { AllowedFileShare = FileShare.None });

        Assert.Equal(DiagnosticCodes.ModApplyFailed, (await Apply(Copy())).Error!.Code);
    }

    [Fact]
    public async Task EditAddInsList_AddsFromTheManifestAndRemovesOnlyTheNamedEntry()
    {
        var added = await Edit(new AddInsListChange("qwinn_fixpack_3", "fixpack", Add: true));
        var addedAgain = await Edit(new AddInsListChange("QWINN_FIXPACK_3", "fixpack", Add: true));
        var xml = ReadAddIns();

        Assert.True(added.IsSuccess && addedAgain.IsSuccess);
        Assert.Equal(2, xml.Split("qwinn_fixpack_3").Length);
        Assert.Contains("Name=\"QwinnFixpack3\"", xml, StringComparison.Ordinal);
        Assert.Contains("users_own", xml, StringComparison.Ordinal);

        await Edit(new AddInsListChange("qwinn_fixpack_3", "fixpack", Add: false));
        await Edit(new AddInsListChange("qwinn_fixpack_3", "fixpack", Add: false));

        Assert.DoesNotContain("qwinn_fixpack_3", ReadAddIns(), StringComparison.Ordinal);
        Assert.Contains("users_own", ReadAddIns(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("\r\n", "<?xml version=\"1.0\" encoding=\"utf-8\"?>")]
    [InlineData("\n", "<?xml version=\"1.0\" encoding=\"utf-8\"?>")]
    [InlineData("\r\n", "<?xml version='1.0' encoding='UTF-8' ?>")]
    [InlineData("\r\n", "")]
    public async Task EditAddInsList_AddThenRemove_GivesBackTheSameBytes(string newLine, string declaration)
    {
        var original = $"{declaration}{(declaration.Length > 0 ? newLine : string.Empty)}<AddInsList>{newLine}  <AddInItem UID=\"users_own\" Name=\"Mine\" Enabled=\"1\" />{newLine}</AddInsList>";
        _scenario.Write(Documents, "Settings/AddIns.xml", original);

        await Edit(new AddInsListChange("qwinn_fixpack_3", "fixpack", Add: true));
        await Edit(new AddInsListChange("qwinn_fixpack_3", "fixpack", Add: false));

        Assert.Equal(original, ReadAddIns());
    }

    [Fact]
    public async Task EditAddInsList_NoFileYet_CreatesOne()
    {
        _scenario.FileSystem.File.Delete(Full(Documents, "Settings/AddIns.xml"));

        Assert.True((await Edit(new AddInsListChange("qwinn_fixpack_3", "fixpack", Add: true))).IsSuccess);
        Assert.Contains("<AddInsList><AddInItem UID=\"qwinn_fixpack_3\"", ReadAddIns(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task EditAddInsList_KeepsAByteOrderMarkWhenTheFileHadOne()
    {
        var withBom = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes("<AddInsList />")).ToArray();
        _scenario.FileSystem.File.WriteAllBytes(Full(Documents, "Settings/AddIns.xml"), withBom);

        await Edit(new AddInsListChange("qwinn_fixpack_3", "fixpack", Add: true));

        Assert.True(_scenario.FileSystem.File.ReadAllBytes(Full(Documents, "Settings/AddIns.xml")).AsSpan().StartsWith(Encoding.UTF8.Preamble));
    }

    [Theory]
    [InlineData("<NotAnAddInsList />", "does not have an AddInsList")]
    [InlineData("<AddInsList", "could not be read")]
    public async Task EditAddInsList_UnexpectedFile_IsLeftAlone(string content, string message)
    {
        _scenario.Write(Documents, "Settings/AddIns.xml", content);

        var result = await Edit(new AddInsListChange("qwinn_fixpack_3", "fixpack", Add: true));

        Assert.Contains(message, result.Error!.Message, StringComparison.Ordinal);
        Assert.Equal(content, ReadAddIns());
    }

    [Fact]
    public async Task EditAddInsList_ManifestWithoutTheItem_Fails()
    {
        var result = await Edit(new AddInsListChange("some_other_uid", "fixpack", Add: true));

        Assert.Contains("has no AddInItem some_other_uid", result.Error!.Message, StringComparison.Ordinal);
    }

    private static PlanOperation Copy() =>
        new(PlanAction.Copy, Hair, "hair", new DeployedFile("hair", new LibraryModFile("textures/hair.dds", 4, Sha("hair")), Hair, IsOverride: true), null);

    private Task<DAORework.Core.Common.Result> Apply(PlanOperation operation) => _scenario.Writer.ApplyAsync(Documents, operation);

    private Task<DAORework.Core.Common.Result> Edit(AddInsListChange change) => _scenario.Writer.EditAddInsListAsync(Documents, [change]);

    private string Read(string relative) => _scenario.FileSystem.File.ReadAllText(Full(Documents, relative));

    private string ReadAddIns() => Read("Settings/AddIns.xml");

    private string Full(string root, string relative) => _scenario.Full(root, relative);
}
