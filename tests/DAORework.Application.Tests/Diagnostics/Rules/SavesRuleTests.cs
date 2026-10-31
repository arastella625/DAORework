using System.IO.Abstractions.TestingHelpers;
using DAORework.Application.Diagnostics.Rules;
using DAORework.Application.Tests.Fakes;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Profiles;
using Xunit;

namespace DAORework.Application.Tests.Diagnostics.Rules;

public sealed class SavesRuleTests
{
    private static readonly string _documents = TestPaths.Root("Docs", "BioWare", "Dragon Age");

    private readonly MockFileSystem _fileSystem = new();

    private static string Characters => GamePaths.CharactersDirectory(_documents);

    [Fact]
    public async Task EvaluateAsync_NoCharactersFolder_SaysNoSaves()
    {
        _fileSystem.AddDirectory(_documents);

        Assert.Equal(DiagnosticCodes.SavesNone, Assert.Single(await Evaluate()).Code);
    }

    [Fact]
    public async Task EvaluateAsync_HealthySaves_CountsThemPerCharacter()
    {
        AddSave("Aeducan", 1, 4096);
        AddSave("Aeducan", 2, 4096);
        AddSave("Cousland", 1, 2048);

        var issue = Assert.Single(await Evaluate());

        Assert.Equal(DiagnosticCodes.SavesFound, issue.Code);
        Assert.Contains("3 saved game(s) for 2 character(s)", issue.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EvaluateAsync_EmptySaveFile_WarnsWithItsPath()
    {
        AddSave("Aeducan", 1, 4096);
        AddSave("Aeducan", 2, 0);

        var issues = await Evaluate();

        var empty = Assert.Single(issues, i => i.Code == DiagnosticCodes.SavesEmptyFile);
        Assert.Equal(DiagnosticSeverity.Warning, empty.Severity);
        Assert.Contains("Slot_2", empty.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EvaluateAsync_TooManySaves_Warns()
    {
        for (var slot = 0; slot <= SavesRule.ManySavesThreshold; slot++)
        {
            AddSave("Hoarder", slot, 16);
        }

        Assert.Contains(await Evaluate(), i => i.Code == DiagnosticCodes.SavesMany);
    }

    private void AddSave(string character, int slot, int bytes) =>
        _fileSystem.AddFile(Path.Combine(Characters, character, "Saves", $"Slot_{slot}", $"Slot_{slot}.das"), new MockFileData(new byte[bytes]));

    private Task<IReadOnlyList<DiagnosticIssue>> Evaluate() =>
        new SavesRule(_fileSystem).EvaluateAsync(new RuntimeProfile { UserDocumentsDirectory = _documents });
}
