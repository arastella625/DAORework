using System.IO.Abstractions.TestingHelpers;
using System.IO.Compression;
using System.Text;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using DAORework.App.ViewModels;
using DAORework.App.Views;
using Xunit;

namespace DAORework.App.Tests;

/// <summary>What active mods share inside their archives shows on the Combined files card (P3-07).</summary>
public sealed class PackedConflictsScreenTests : IDisposable
{
    private readonly AppTestHost _host = new();
    private readonly ModsTabScenario _scenario;

    public PackedConflictsScreenTests()
    {
        _scenario = new ModsTabScenario(_host);
    }

    public void Dispose() => _host.Dispose();

    [AvaloniaFact]
    public async Task TwoModsHoldingOneResourceInsideArchives_AreNamed_AndNeitherIsSaidToWin()
    {
        var (window, viewModel) = await _scenario.OpenAsync();
        await ImportAsync(viewModel, "First Pack", "first.erf", "shared_head.mmh");
        await ImportAsync(viewModel, "Second Pack", "second.erf", "shared_head.mmh");
        await _scenario.ActivateAsync(viewModel, "First Pack");
        await _scenario.ActivateAsync(viewModel, "Second Pack");

        var row = Assert.Single(viewModel.ModsTab.LoadOrder.PackedConflicts);

        Assert.Equal("shared_head.mmh", row.FileName);
        Assert.Contains("First Pack, Second Pack", row.Detail, StringComparison.Ordinal);
        Assert.Contains("can't tell which", row.Detail, StringComparison.Ordinal);
        Assert.Contains("turn one of these mods off", row.Detail, StringComparison.Ordinal);
        Assert.Equal("1 only changes how things look.", viewModel.ModsTab.LoadOrder.SharedSummary);
        ModsTabScenario.Show<CombinedFilesPanel>(window);
        Assert.False(window.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Name == "NoSharedFiles").IsVisible);
        Screenshots.Save(window, "40-packed-conflicts");
    }

    private async Task ImportAsync(MainWindowViewModel viewModel, string name, string archive, string resource)
    {
        var path = MockUnixSupport.Path($@"C:\Users\Me\Downloads\{name}.zip");
        using var buffer = new MemoryStream();
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            await using var entry = await zip.CreateEntry(archive).OpenAsync(TestContext.Current.CancellationToken);
            entry.Write(Erf(resource));
        }

        _host.FileSystem.AddFile(path, new MockFileData(buffer.ToArray()));
        await viewModel.Library.ImportPathsAsync([path], TestContext.Current.CancellationToken);
    }

    private static byte[] Erf(string resource)
    {
        using var buffer = new MemoryStream();
        using var writer = new BinaryWriter(buffer);
        writer.Write(Encoding.Unicode.GetBytes("ERF V2.0"));
        writer.Write(1u);
        writer.Write(new byte[12]);
        var name = new byte[64];
        Encoding.Unicode.GetBytes(resource).CopyTo(name, 0);
        writer.Write(name);
        writer.Write(104u);
        writer.Write(1u);
        writer.Write((byte)7);
        return buffer.ToArray();
    }
}
