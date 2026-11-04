using DAORework.Application.Abstractions;
using DAORework.Application.Mods.Downloads;
using DAORework.Application.Tests.Fakes;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Downloads;
using Xunit;

namespace DAORework.Application.Tests.Mods.Downloads;

/// <summary>Finding new mods in the downloads folder (ADR-0034 §1 to §5).</summary>
public sealed class FindNewDownloadsTests
{
    private static readonly DateTimeOffset _on = new(2026, 11, 4, 12, 0, 0, TimeSpan.Zero);
    private static readonly DownloadFile _mod = new("E:/Get/Blue Hair-1-1-0.zip", 5, _on.AddHours(1), _on.AddHours(1));
    private static readonly DownloadFile _photos = new("E:/Get/photos.zip", 5, _on.AddHours(1), _on.AddHours(1));

    private readonly FakeFolder _folder = new();
    private readonly FakeStore _store = new();
    private readonly FakeKnownFolders _known = new() { Downloads = "E:/Get" };

    private FindNewDownloads Find => new(_folder, _store, _known);

    [Fact]
    public async Task Execute_TurnedOff_LooksAtNothing()
    {
        var found = await Find.ExecuteAsync(new FindNewDownloadsRequest());

        Assert.Empty(found.Value);
        Assert.Equal(0, _folder.Listed);
    }

    [Fact]
    public async Task Execute_TurnedOn_OffersTheMod_ByItsName()
    {
        TurnOn();

        var offered = Assert.Single((await Find.ExecuteAsync(new FindNewDownloadsRequest())).Value);

        Assert.Equal("Blue Hair", offered.Name);
    }

    [Fact]
    public async Task Execute_AnArchiveThatIsNoMod_IsOpenedOnce()
    {
        TurnOn();

        await Find.ExecuteAsync(new FindNewDownloadsRequest());
        var opened = _folder.Opened.Count(p => p == _photos.Path);
        await Find.ExecuteAsync(new FindNewDownloadsRequest());

        Assert.Equal(1, opened);
        Assert.Equal(1, _folder.Opened.Count(p => p == _photos.Path));
    }

    [Fact]
    public async Task Execute_AnArchiveThatCannotBeListed_IsLeftOut_AndTriedAgain()
    {
        TurnOn();
        _folder.Broken = true;

        Assert.Empty((await Find.ExecuteAsync(new FindNewDownloadsRequest())).Value);

        _folder.Broken = false;
        Assert.Single((await Find.ExecuteAsync(new FindNewDownloadsRequest())).Value);
    }

    [Fact]
    public async Task Execute_ASavedFolder_IsUsed_OverTheOneWindowsReports()
    {
        _store.Settings = NewDownloadsSettings.Off with { Enabled = true, Folder = "F:/Mine", EnabledAtUtc = _on };

        await Find.ExecuteAsync(new FindNewDownloadsRequest());

        Assert.Equal("F:/Mine", _folder.LastFolder);
    }

    [Fact]
    public async Task Execute_NoFolderAtAll_SaysSo()
    {
        var find = new FindNewDownloads(_folder, _store, new FakeKnownFolders());

        var result = await find.ExecuteAsync(new FindNewDownloadsRequest(Asked: true));

        Assert.Equal(DiagnosticCodes.DownloadsFolderMissing, result.Error!.Code);
    }

    [Fact]
    public async Task Execute_Asked_WorksWithItOff_AndSavesNothing()
    {
        var found = await Find.ExecuteAsync(new FindNewDownloadsRequest(Asked: true));

        Assert.Single(found.Value);
        Assert.Equal(0, _store.Saves);
    }

    [Fact]
    public async Task Execute_AnEntryForAFileThatIsGone_IsDropped()
    {
        TurnOn();
        _store.Settings = _store.Settings with { Handled = [new HandledDownload("E:/Get/gone.zip", 1, _on)] };

        await Find.ExecuteAsync(new FindNewDownloadsRequest());

        Assert.DoesNotContain(_store.Settings.Handled, h => h.Path == "E:/Get/gone.zip");
    }

    [Fact]
    public async Task SetDownloadHandled_IsNotOfferedAgain()
    {
        TurnOn();
        await new SetDownloadHandled(_store).ExecuteAsync(_mod);

        Assert.Empty((await Find.ExecuteAsync(new FindNewDownloadsRequest())).Value);
    }

    [Fact]
    public async Task SetNewDownloads_TurningOn_NotesTheMoment_AndKeepsItWhileOn()
    {
        var clock = new FixedClock(_on);
        var set = new SetNewDownloads(_store, clock);

        var on = (await set.ExecuteAsync(enabled: true, folder: null)).Value;
        clock.Now = _on.AddDays(1);
        var again = (await set.ExecuteAsync(enabled: true, folder: "F:/Mine")).Value;

        Assert.Equal(_on, on.EnabledAtUtc);
        Assert.True(on.Asked);
        Assert.Equal(_on, again.EnabledAtUtc);
    }

    private void TurnOn() => _store.Settings = NewDownloadsSettings.Off with { Enabled = true, EnabledAtUtc = _on };

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;

        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class FakeFolder : IDownloadsFolder
    {
        public int Listed { get; private set; }

        public string? LastFolder { get; private set; }

        public List<string> Opened { get; } = [];

        public bool Broken { get; set; }

        public Result<IReadOnlyList<DownloadFile>> ListFinished(string folder)
        {
            Listed++;
            LastFolder = folder;
            return Result.Success<IReadOnlyList<DownloadFile>>([_mod, _photos]);
        }

        public Result<IReadOnlyList<string>> ReadEntryNames(string path)
        {
            Opened.Add(path);
            return Broken
                ? Result.Failure<IReadOnlyList<string>>(new Error(DiagnosticCodes.DownloadsUnreadable, "no"))
                : Result.Success<IReadOnlyList<string>>(path == _mod.Path ? ["hair.erf"] : ["photo.jpg"]);
        }
    }

    private sealed class FakeStore : INewDownloadsStore
    {
        public NewDownloadsSettings Settings { get; set; } = NewDownloadsSettings.Off;

        public int Saves { get; private set; }

        public Task<Result<NewDownloadsSettings>> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(Result.Success(Settings));

        public Task SaveAsync(NewDownloadsSettings settings, CancellationToken cancellationToken = default)
        {
            Settings = settings;
            Saves++;
            return Task.CompletedTask;
        }
    }
}
