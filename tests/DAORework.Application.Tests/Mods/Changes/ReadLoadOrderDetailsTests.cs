using DAORework.Application.Abstractions;
using DAORework.Application.Mods.Changes;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Library;
using Xunit;

namespace DAORework.Application.Tests.Mods.Changes;

/// <summary>Add-in archives count as much as override ones (P3-07): the fix packs keep their changes in them.</summary>
public sealed class ReadLoadOrderDetailsTests
{
    private const string FixArchive = "Contents/addins/fixpack/module/data/fixpack_module.erf";

    private static readonly LibraryMod _fixpack = new("fixpack", "Fixpack", ModKind.AddIn, [File(FixArchive), File("Manifest.xml")], "fixpack");
    private static readonly LibraryMod _kiss = new("camp-kiss", "Camp Kiss", ModKind.Override, [File("camp_kiss.cub")]);

    private readonly Dictionary<string, IReadOnlyList<string>> _archives = new() { [FixArchive] = ["camp_kiss.cub", "other.dlg"] };

    [Fact]
    public async Task Execute_AnAddInsArchiveAndAnOverrideFile_AreCompared()
    {
        var details = await UseCase(_fixpack, _kiss).ExecuteAsync(["fixpack", "camp-kiss"]);

        var conflict = Assert.Single(details.Packed);
        Assert.Equal("camp_kiss.cub", conflict.FileName);
        Assert.Equal(["fixpack", "camp-kiss"], conflict.ModIds);
    }

    [Fact]
    public async Task Execute_TwoAddInsArchives_AreCompared()
    {
        const string otherArchive = "Contents/addins/other/core/data/other_modcore.erf";
        _archives[otherArchive] = ["other.dlg"];
        var other = new LibraryMod("other", "Other", ModKind.AddIn, [File(otherArchive)], "other");

        var details = await UseCase(_fixpack, other).ExecuteAsync(["fixpack", "other"]);

        Assert.Equal("other.dlg", Assert.Single(details.Packed).FileName);
    }

    [Fact]
    public async Task Execute_AnAddInsFileOutsideOverride_IsNotComparedByName()
    {
        var sound = new LibraryMod("voices", "Voices", ModKind.AddIn, [File("Contents/addins/voices/core/audio/camp_kiss.cub")], "voices");

        var details = await UseCase(_fixpack, sound).ExecuteAsync(["fixpack", "voices"]);

        Assert.Empty(details.Packed);
    }

    [Fact]
    public async Task Execute_AModThatIsntActive_IsLeftOut()
    {
        var details = await UseCase(_fixpack, _kiss).ExecuteAsync(["fixpack"]);

        Assert.Empty(details.Packed);
    }

    [Fact]
    public async Task Execute_AnArchiveThatCantBeRead_IsLeftOut()
    {
        _archives.Clear();

        var details = await UseCase(_fixpack, _kiss).ExecuteAsync(["fixpack", "camp-kiss"]);

        Assert.Empty(details.Packed);
    }

    private static LibraryModFile File(string path) => new(path, 1, "h");

    private ReadLoadOrderDetails UseCase(params LibraryMod[] mods) => new(new FakeLibrary(mods), new FakeErf(_archives));

    private sealed class FakeErf(Dictionary<string, IReadOnlyList<string>> archives) : IErfContentsReader
    {
        public Result<IReadOnlyList<string>> ReadNames(string modId, string relativePath) =>
            archives.TryGetValue(relativePath, out var names)
                ? Result.Success(names)
                : Result.Failure<IReadOnlyList<string>>(new Error(DiagnosticCodes.ModErfUnreadable, "unreadable"));
    }

    private sealed class FakeLibrary(IReadOnlyList<LibraryMod> mods) : IModLibrary
    {
        public Task<IReadOnlyList<LibraryEntry>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<LibraryEntry>>([.. mods.Select(m => new LibraryEntry(m, m.Name, "s", DateTimeOffset.UnixEpoch))]);

        public Task<Result<LibraryImport>> ImportAsync(string sourcePath, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<Result<string>> ReadTextAsync(string modId, string relativePath, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<Result<LibraryModFile>> StoreCombinedAsync(string fileName, string text, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
