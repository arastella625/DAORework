using DAORework.Application.Preferences;
using DAORework.Application.Tests.Fakes;
using DAORework.Core.Preferences;
using Xunit;

namespace DAORework.Application.Tests.Preferences;

public sealed class SetLookTests
{
    [Fact]
    public async Task SetLook_SavesThePick_AndKeepsTheProfileLooks()
    {
        var store = new FakePreferencesStore
        {
            Saved = new AppPreferences { Look = "korcari", ProfileLooks = new Dictionary<string, string> { ["Wilds run"] = "the-fade" } },
        };

        var result = await new SetLook(store).ExecuteAsync(new SetLookRequest("blood-and-shadow"));

        Assert.True(result.IsSuccess);
        Assert.Equal("blood-and-shadow", store.Saved.Look);
        Assert.Equal("the-fade", store.Saved.ProfileLooks["Wilds run"]);
    }

    [Fact]
    public async Task SetLook_OverAnUnreadableFile_ReplacesItWithThePick()
    {
        var store = new FakePreferencesStore { Unreadable = true };

        var result = await new SetLook(store).ExecuteAsync(new SetLookRequest("the-fade"));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, store.Saves);
        Assert.Equal("the-fade", store.Saved.Look);
    }

    [Fact]
    public async Task GetPreferences_PassesAnUnreadableFileOnAsAFailure()
    {
        var store = new FakePreferencesStore { Unreadable = true };

        var result = await new GetPreferences(store).ExecuteAsync(new GetPreferencesRequest());

        Assert.True(result.IsFailure);
    }
}
