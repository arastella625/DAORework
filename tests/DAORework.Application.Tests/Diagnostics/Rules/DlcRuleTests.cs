using System.IO.Abstractions.TestingHelpers;
using DAORework.Application.Diagnostics.Rules;
using DAORework.Application.Tests.Fakes;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Profiles;
using Xunit;

namespace DAORework.Application.Tests.Diagnostics.Rules;

public sealed class DlcRuleTests
{
    private const string AddIns = """
        <?xml version="1.0" encoding="utf-8"?>
        <AddInsList>
          <AddInItem UID="DAO_PRC_CP_2" Name="dao_prc_cp_2" Enabled="1">
            <Title DefaultText="The Stone Prisoner" />
          </AddInItem>
          <AddInItem UID="DAO_PRC_EP_1" Name="dao_prc_ep_1" Enabled="1">
            <Title DefaultText="Awakening" />
          </AddInItem>
          <AddInItem UID="SOME_MOD" Name="Some Mod" Enabled="0" />
        </AddInsList>
        """;

    private static readonly string _documents = TestPaths.Root("Docs", "BioWare", "Dragon Age");

    private readonly MockFileSystem _fileSystem = new();

    [Fact]
    public async Task EvaluateAsync_NoAddInsList_SaysNone()
    {
        _fileSystem.AddDirectory(_documents);

        Assert.Equal(DiagnosticCodes.DlcNone, Assert.Single(await Evaluate()).Code);
    }

    [Fact]
    public async Task EvaluateAsync_ListsEnabledTitlesAndCountsDisabled()
    {
        _fileSystem.AddFile(GamePaths.AddInsListFile(_documents), new MockFileData(AddIns));

        var issue = Assert.Single(await Evaluate());

        Assert.Equal(DiagnosticCodes.DlcFound, issue.Code);
        Assert.Equal("3 add-in(s) registered: Awakening, The Stone Prisoner (1 disabled).", issue.Message);
    }

    [Fact]
    public async Task EvaluateAsync_MalformedXml_WarnsUnreadable()
    {
        _fileSystem.AddFile(GamePaths.AddInsListFile(_documents), new MockFileData("<AddInsList><AddInItem"));

        var issue = Assert.Single(await Evaluate());

        Assert.Equal(DiagnosticCodes.DlcListUnreadable, issue.Code);
        Assert.Equal(DiagnosticSeverity.Warning, issue.Severity);
    }

    private Task<IReadOnlyList<DiagnosticIssue>> Evaluate() =>
        new DlcRule(_fileSystem).EvaluateAsync(new RuntimeProfile { UserDocumentsDirectory = _documents });
}
