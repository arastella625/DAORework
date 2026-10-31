using DAORework.Application.Looks;
using DAORework.Application.Tests.Fakes;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Looks;
using Xunit;

namespace DAORework.Application.Tests.Looks;

/// <summary>Giving a look a background picture, adjusting it within its floor, and taking it away (ADR-0017).</summary>
public sealed class LookBackgroundTests
{
    private readonly FakeLookStore _looks = new();
    private readonly FakeBackgroundImageImporter _importer = new();

    public LookBackgroundTests() => _looks.Looks["look-a"] = new Look { Id = "look-a", Name = "Wilds run", BaseTheme = "korcari" };

    [Fact]
    public async Task Set_RecordsTheCopyAndItsBrightness_AtTheDefaults()
    {
        var set = await new SetLookBackground(_looks, _importer).ExecuteAsync(new SetLookBackgroundRequest("look-a", @"C:\Pictures\swamp.jpg"));

        var background = set.Value.Background!;
        Assert.Equal("background.png", background.FileName);
        Assert.Equal("#D8C9A0", background.Brightest);
        Assert.Equal((BackgroundFit.Fill, 40, 80, 6), (background.Fit, background.Tint, background.Dim, background.Blur));
        Assert.Same(set.Value, _looks.Looks["look-a"]);
    }

    [Fact]
    public async Task Set_AReplacement_KeepsTheSettings()
    {
        _looks.Looks["look-a"] = _looks.Looks["look-a"] with { Background = new LookBackground { FileName = "background.png", Brightest = "#FFFFFF", Tint = 70, Dim = 90, Blur = 12 } };

        var set = await new SetLookBackground(_looks, _importer).ExecuteAsync(new SetLookBackgroundRequest("look-a", "next.png"));

        Assert.Equal((70, 90, 12, "#D8C9A0"), (set.Value.Background!.Tint, set.Value.Background.Dim, set.Value.Background.Blur, set.Value.Background.Brightest));
    }

    [Fact]
    public async Task Set_APictureThatCannotBeUsed_LeavesTheLookAsItWas()
    {
        _importer.Next = Result.Failure<Abstractions.ImportedPicture>(new Error(DiagnosticCodes.LookImageTooLarge, "Too large."));

        var set = await new SetLookBackground(_looks, _importer).ExecuteAsync(new SetLookBackgroundRequest("look-a", "huge.png"));

        Assert.Equal(DiagnosticCodes.LookImageTooLarge, set.Error!.Code);
        Assert.Null(_looks.Looks["look-a"].Background);
    }

    [Fact]
    public async Task Adjust_KeepsEachValueInRange_AndTheDimAtOrAboveTheFloor()
    {
        await new SetLookBackground(_looks, _importer).ExecuteAsync(new SetLookBackgroundRequest("look-a", "swamp.jpg"));

        var adjusted = await new AdjustLookBackground(_looks).ExecuteAsync(new AdjustLookBackgroundRequest("look-a", BackgroundFit.Fit, Tint: 140, Dim: 30, Blur: 99, DimFloor: 72));

        var background = adjusted.Value.Background!;
        Assert.Equal((BackgroundFit.Fit, 100, 72, LookBackground.MaxBlur), (background.Fit, background.Tint, background.Dim, background.Blur));
    }

    [Fact]
    public async Task Adjust_ALookWithNoPicture_SaysSo()
    {
        var adjusted = await new AdjustLookBackground(_looks).ExecuteAsync(new AdjustLookBackgroundRequest("look-a", BackgroundFit.Fill, 40, 80, 6, 0));

        Assert.Equal(DiagnosticCodes.LookBackgroundMissing, adjusted.Error!.Code);
    }

    [Fact]
    public async Task Load_ReturnsTheLookWithItsPicture()
    {
        await new SetLookBackground(_looks, _importer).ExecuteAsync(new SetLookBackgroundRequest("look-a", "swamp.jpg"));

        var loaded = await new LoadLook(_looks, _importer).ExecuteAsync(new LoadLookRequest("look-a"));

        Assert.Equal([1, 2, 3], loaded.Value.Picture);
        Assert.Null(loaded.Value.PictureError);
    }

    [Fact]
    public async Task Load_ALookWhosePictureIsGone_StillLoads_AndSaysWhy()
    {
        await new SetLookBackground(_looks, _importer).ExecuteAsync(new SetLookBackgroundRequest("look-a", "swamp.jpg"));
        _importer.Picture = Result.Failure<byte[]>(new Error(DiagnosticCodes.LookImageUnreadable, "Missing."));

        var loaded = await new LoadLook(_looks, _importer).ExecuteAsync(new LoadLookRequest("look-a"));

        Assert.Equal("look-a", loaded.Value.Look.Id);
        Assert.Null(loaded.Value.Picture);
        Assert.Equal(DiagnosticCodes.LookImageUnreadable, loaded.Value.PictureError!.Code);
    }

    [Fact]
    public async Task Remove_TakesThePictureAway_AndDeletesTheCopy()
    {
        await new SetLookBackground(_looks, _importer).ExecuteAsync(new SetLookBackgroundRequest("look-a", "swamp.jpg"));

        var removed = await new RemoveLookBackground(_looks, _importer).ExecuteAsync(new RemoveLookBackgroundRequest("look-a"));

        Assert.Null(removed.Value.Background);
        Assert.Equal([("look-a", "background.png")], _importer.Removed);
    }
}
