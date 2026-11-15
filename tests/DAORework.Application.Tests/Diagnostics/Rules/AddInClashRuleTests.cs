using DAORework.Application.Abstractions;
using DAORework.Application.Diagnostics.Rules;
using DAORework.Application.Mods.AddIns;
using DAORework.Application.Tests.Fakes;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.AddIns;
using DAORework.Core.Profiles;
using Xunit;

namespace DAORework.Application.Tests.Diagnostics.Rules;

/// <summary>Files add-ins share (P2.5-23): each a warning naming whose copy the game uses.</summary>
public sealed class AddInClashRuleTests
{
    private static readonly RuntimeProfile _profile = new() { UserDocumentsDirectory = TestPaths.Root("Docs") };

    [Fact]
    public async Task EvaluateAsync_NoSharedFiles_ReportsNothing()
    {
        var reader = new StubReader(Result.Success(new DocumentsAddIns([AddIn("a", 100, 0)], [Contents(AddIn("a", 100, 0), "a.ncs")], [])));

        Assert.Empty(await Rule(reader).EvaluateAsync(_profile));
    }

    [Fact]
    public async Task EvaluateAsync_ASharedFile_IsAWarningNamingTheWinner()
    {
        var fixpack = AddIn("Qwinn's Fixpack", 109, 0);
        var dog = AddIn("Extra Dog Slot", 100, 1);
        var reader = new StubReader(Result.Success(new DocumentsAddIns([fixpack, dog], [Contents(fixpack, "party.ncs"), Contents(dog, "party.ncs")], [])));

        var issue = Assert.Single(await Rule(reader).EvaluateAsync(_profile));

        Assert.Equal(DiagnosticCodes.AddInsShareFile, issue.Code);
        Assert.Equal(DiagnosticSeverity.Warning, issue.Severity);
        Assert.StartsWith("party.ncs is in both Extra Dog Slot and Qwinn's Fixpack. The game uses Extra Dog Slot's copy", issue.Message, StringComparison.Ordinal);
        Assert.Equal(AddInClashWords.Advice, issue.Recommendation);
    }

    [Fact]
    public async Task EvaluateAsync_ManySharedFiles_ListsTenAndCountsTheRest_AndSaysWhatCouldNotBeRead()
    {
        var names = Enumerable.Range(0, 12).Select(i => $"f{i}.dds").ToArray();
        var a = AddIn("a", 100, 0);
        var b = AddIn("b", 100, 1);
        var reader = new StubReader(Result.Success(new DocumentsAddIns([a, b], [Contents(a, names), Contents(b, names)], ["AddIns/b/core/data/broken.erf"])));

        var issues = await Rule(reader).EvaluateAsync(_profile);

        Assert.Equal(11, issues.Count(i => i.Code == DiagnosticCodes.AddInsShareFile));
        Assert.Contains(issues, i => i.Message == "2 more files are in more than one add-in.");
        Assert.Contains(issues, i => i.Code == DiagnosticCodes.AddInsArchiveUnreadable && i.Message.Contains("1 archive in AddIns couldn't be read", StringComparison.Ordinal));
    }

    [Fact]
    public async Task EvaluateAsync_AnUnreadableList_IsReportedNotThrown_AndNoDocumentsFolderIsNothing()
    {
        var error = new Error(DiagnosticCodes.AddInsListUnreadable, "Settings/AddIns.xml isn't valid XML.");
        var rule = Rule(new StubReader(Result.Failure<DocumentsAddIns>(error)));

        Assert.Equal(DiagnosticCodes.AddInsListUnreadable, Assert.Single(await rule.EvaluateAsync(_profile)).Code);
        Assert.Empty(await rule.EvaluateAsync(new RuntimeProfile()));
    }

    private static AddInClashRule Rule(IDocumentsAddInsReader reader) => new(new FindAddInClashes(reader));

    private static ListedAddIn AddIn(string name, int priority, int position) => new(name, name, "Single Player", priority, position, false);

    private static AddInContents Contents(ListedAddIn addIn, params string[] core) => new(addIn, core, []);

    private sealed class StubReader(Result<DocumentsAddIns> result) : IDocumentsAddInsReader
    {
        public Task<Result<DocumentsAddIns>> ReadAsync(string documentsDirectory, CancellationToken cancellationToken = default) => Task.FromResult(result);
    }
}
