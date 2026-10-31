using System.Buffers.Binary;
using System.IO.Abstractions.TestingHelpers;
using System.Text;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using DAORework.App.ViewModels;
using DAORework.App.Views;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Profiles;
using Xunit;

namespace DAORework.App.Tests;

/// <summary>The known-items catalog (P1-07) on the Diagnostics page, against the catalog that ships.</summary>
public sealed class KnownItemsScreenTests : IDisposable
{
    private const ushort X86 = 0x014C;
    private const ushort X64 = 0x8664;

    private readonly AppTestHost _host = new();

    public void Dispose() => _host.Dispose();

    [AvaloniaFact]
    public async Task DiagnosticsPage_ListsDxvkWithItsVersionAndTheFourGigabytePatch()
    {
        _host.SeedRealisticInstall();
        SeedGame(dxvkMachine: X86);
        var (window, viewModel) = await OpenWithProfileAsync();

        await viewModel.Diagnostics.RunDiagnosticsCommand.ExecuteAsync(null);

        var found = Assert.Single(viewModel.Diagnostics.Issues, i => i.Code == DiagnosticCodes.KnownItemsFound);
        Assert.Contains("DXVK v2.3", found.Message, StringComparison.Ordinal);
        Assert.Contains("4 GB patch", found.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(viewModel.Diagnostics.Issues, i => i.Code == DiagnosticCodes.KnownItemSuggested);
        ScrollIssuesToEnd(window);
        Screenshots.Save(window, "10-known-items");
    }

    [AvaloniaFact]
    public async Task DiagnosticsPage_Warns64BitDxvkAndSuggestsNothingElse()
    {
        _host.SeedRealisticInstall();
        SeedGame(dxvkMachine: X64);
        var (window, viewModel) = await OpenWithProfileAsync();

        await viewModel.Diagnostics.RunDiagnosticsCommand.ExecuteAsync(null);

        var warning = Assert.Single(viewModel.Diagnostics.Issues, i => i.Code == DiagnosticCodes.KnownItemWrongArchitecture);
        Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
        Assert.True(viewModel.Diagnostics.CanLaunch);
        ScrollIssuesToEnd(window);
        Screenshots.Save(window, "11-dxvk-64-bit");
    }

    /// <summary>The catalog's issues come last in the list; scroll there so the screenshot shows them.</summary>
    private static void ScrollIssuesToEnd(MainWindow window)
    {
        window.UpdateLayout();
        var issues = window.GetVisualDescendants().OfType<ItemsControl>().Single(c => c.Name == "Issues");
        var scroller = issues.FindAncestorOfType<ScrollViewer>() ?? throw new InvalidOperationException("The issue list is not in a ScrollViewer.");
        scroller.ScrollToEnd();
        window.UpdateLayout();
    }

    /// <summary>A real-looking game executable with the 4 GB flag set, and DXVK's d3d9.dll built for <paramref name="dxvkMachine"/>.</summary>
    private void SeedGame(ushort dxvkMachine)
    {
        _host.FileSystem.AddFile(GamePaths.GameExecutable(AppTestHost.InstallDirectory), new MockFileData(Pe(X86, 0x0122)));
        var dxvk = Pe(dxvkMachine, 0x2102).Concat(Encoding.ASCII.GetBytes("\0Game: \0DXVK: \0v2.3\0Build: \0")).ToArray();
        _host.FileSystem.AddFile(Path.Combine(AppTestHost.InstallDirectory, "bin_ship", "d3d9.dll"), new MockFileData(dxvk));
    }

    private static byte[] Pe(ushort machine, ushort characteristics)
    {
        var bytes = new byte[512];
        "MZ"u8.CopyTo(bytes);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(0x3C), 0x80);
        "PE\0\0"u8.CopyTo(bytes.AsSpan(0x80));
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(0x84), machine);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(0x80 + 22), characteristics);
        return bytes;
    }

    private async Task<(MainWindow Window, MainWindowViewModel ViewModel)> OpenWithProfileAsync()
    {
        var viewModel = _host.Resolve<MainWindowViewModel>();
        var window = new MainWindow { DataContext = viewModel };
        window.Show();
        await viewModel.InitializeAsync(TestContext.Current.CancellationToken);

        viewModel.Profiles.NewProfileName = "Default";
        await viewModel.Profiles.CreateProfileCommand.ExecuteAsync(null);
        await viewModel.SelectProfileAsync("Default", TestContext.Current.CancellationToken);
        _host.Picker.Enqueue(AppTestHost.InstallDirectory);
        await viewModel.Profile.Paths.BrowseCommand.ExecuteAsync(ProfilePathKind.Install);
        _host.Picker.Enqueue(GamePaths.GameExecutable(AppTestHost.InstallDirectory));
        await viewModel.Profile.Paths.BrowseCommand.ExecuteAsync(ProfilePathKind.Game);

        var tabs = window.FindControl<TabControl>("Pages") ?? throw new InvalidOperationException("No tab control named Pages.");
        tabs.SelectedIndex = 1;
        return (window, viewModel);
    }
}
