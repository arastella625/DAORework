using System.IO.Abstractions.TestingHelpers;
using DAORework.App.ViewModels;
using DAORework.App.Views;
using Xunit;

namespace DAORework.App.Tests;

/// <summary>A window with two mods held twice, and the picks a player makes in <i>Keep one of each</i>.</summary>
internal sealed class KeepEachScenario(ModsTabScenario scenario)
{
    private static readonly string _downloads = MockUnixSupport.Path(@"C:\Users\Me\Downloads");

    public static KeepEachRow Set(KeepEachViewModel keep, string title) => keep.Rows.Single(r => r.Title.StartsWith(title, StringComparison.Ordinal));

    public static Task PickOtherCopyAsync(KeepEachViewModel keep, string title)
    {
        var row = Set(keep, title);
        return PickAsync(keep, row, row.Choices.First(c => c.ModId is not null && c != row.Pick));
    }

    public static Task KeepAllAsync(KeepEachViewModel keep, string title) => PickAsync(keep, Set(keep, title), Set(keep, title).Choices[^1]);

    public static async Task PickAsync(KeepEachViewModel keep, KeepEachRow row, KeepChoice choice)
    {
        row.Pick = choice;
        await keep.ReplanCommand.ExecutionTask!;
    }

    public Task ImportAsync(MainWindowViewModel viewModel, string fileName, string file, string content) =>
        scenario.ImportAsync(viewModel, Path.Combine(_downloads, fileName), (file, content));

    public async Task<(MainWindow Window, MainWindowViewModel ViewModel)> OpenAsync()
    {
        var opened = await scenario.OpenAsync();
        await scenario.ImportHairPackAsync(opened.ViewModel);
        await ImportAsync(opened.ViewModel, "Hair Pack Copy.zip", "hair_pack.dds", "DDS hair");
        await ImportAsync(opened.ViewModel, "Eyes.zip", "eyes.dds", "DDS eyes");
        await ImportAsync(opened.ViewModel, "Eyes Copy.zip", "eyes.dds", "DDS eyes");
        Assert.Equal(2, opened.ViewModel.Library.CopySets);
        return opened;
    }
}
