using DAORework.Application.Looks;
using DAORework.Application.Tests.Fakes;
using DAORework.Core.Diagnostics;
using DAORework.Core.Looks;
using DAORework.Core.Preferences;
using Xunit;

namespace DAORework.Application.Tests.Looks;

/// <summary>Creating, editing, listing and deleting custom looks, and a look per profile (ADR-0017).</summary>
public sealed class LookUseCaseTests
{
    private readonly FakeLookStore _looks = new();
    private readonly FakePreferencesStore _preferences = new();

    [Fact]
    public async Task Create_MakesANewLookFromABuiltInTheme()
    {
        var created = await new CreateLook(_looks).ExecuteAsync(new CreateLookRequest("  Wilds run ", "korcari"));

        Assert.True(LookNames.IsValidId(created.Value.Id));
        Assert.Equal("Wilds run", created.Value.Name);
        Assert.Equal("korcari", created.Value.BaseTheme);
        Assert.Null(created.Value.Accent);
        Assert.Same(created.Value, _looks.Looks[created.Value.Id]);
    }

    [Fact]
    public async Task Create_RefusesAnEmptyName()
    {
        var created = await new CreateLook(_looks).ExecuteAsync(new CreateLookRequest(" ", "korcari"));

        Assert.Equal(DiagnosticCodes.LookNameInvalid, created.Error!.Code);
        Assert.Empty(_looks.Looks);
    }

    [Fact]
    public async Task Update_ChangesNameThemeAndAccent_AndKeepsTheBackground()
    {
        var background = new LookBackground { FileName = "background.png", Brightest = "#808080", Dim = 70 };
        _looks.Looks["look-a"] = new Look { Id = "look-a", Name = "Old", BaseTheme = "korcari", Background = background };

        var updated = await new UpdateLook(_looks).ExecuteAsync(new UpdateLookRequest("look-a", "New", "the-fade", "#3ba3d9"));

        Assert.Equal("New", updated.Value.Name);
        Assert.Equal("the-fade", updated.Value.BaseTheme);
        Assert.Equal("#3BA3D9", updated.Value.Accent);
        Assert.Same(background, _looks.Looks["look-a"].Background);
    }

    [Theory]
    [InlineData("#C42B1C", DiagnosticCodes.LookAccentReserved)]
    [InlineData("crimson", DiagnosticCodes.LookColorInvalid)]
    public async Task Update_RefusesARedOrNotAColor(string accent, string code)
    {
        _looks.Looks["look-a"] = new Look { Id = "look-a", Name = "Look", BaseTheme = "korcari" };

        var updated = await new UpdateLook(_looks).ExecuteAsync(new UpdateLookRequest("look-a", "Look", "korcari", accent));

        Assert.Equal(code, updated.Error!.Code);
        Assert.Null(_looks.Looks["look-a"].Accent);
    }

    [Fact]
    public async Task List_SortsByName_AndPassesOnTheUnreadable()
    {
        _looks.Looks["look-b"] = new Look { Id = "look-b", Name = "Shadow", BaseTheme = "korcari" };
        _looks.Looks["look-a"] = new Look { Id = "look-a", Name = "fade", BaseTheme = "korcari" };
        _looks.Unreadable.Add(new(DiagnosticCodes.LookUnreadable, "Bad."));

        var listing = await new ListLooks(_looks).ExecuteAsync(new ListLooksRequest());

        Assert.Equal(["fade", "Shadow"], listing.Value.Looks.Select(look => look.Name));
        Assert.Single(listing.Value.Unreadable);
    }

    [Fact]
    public async Task Delete_SendsEveryoneWhoUsedTheLookBackToTheDefault()
    {
        _looks.Looks["look-a"] = new Look { Id = "look-a", Name = "Look", BaseTheme = "korcari" };
        _preferences.Saved = new AppPreferences
        {
            Look = "look-a",
            ProfileLooks = new Dictionary<string, string> { ["Wilds run"] = "look-a", ["Warden"] = "the-fade" },
        };

        var deleted = await new DeleteLook(_looks, _preferences).ExecuteAsync(new DeleteLookRequest("look-a"));

        Assert.True(deleted.IsSuccess);
        Assert.Empty(_looks.Looks);
        Assert.Null(_preferences.Saved.Look);
        Assert.Equal(["Warden"], _preferences.Saved.ProfileLooks.Keys);
    }

    [Fact]
    public async Task Delete_ALookThatIsNotThere_IsNotFound()
    {
        var deleted = await new DeleteLook(_looks, _preferences).ExecuteAsync(new DeleteLookRequest("look-none"));

        Assert.Equal(DiagnosticCodes.LookNotFound, deleted.Error!.Code);
        Assert.Equal(0, _preferences.Saves);
    }

    [Fact]
    public async Task SetProfileLook_GivesAProfileItsLook_AndNullTakesItAway()
    {
        var set = new SetProfileLook(_preferences);

        await set.ExecuteAsync(new SetProfileLookRequest("Wilds run", "look-a"));
        Assert.Equal("look-a", _preferences.Saved.ProfileLooks["wilds RUN"]);
        await set.ExecuteAsync(new SetProfileLookRequest("Wilds run", null));

        Assert.Empty(_preferences.Saved.ProfileLooks);
    }
}
