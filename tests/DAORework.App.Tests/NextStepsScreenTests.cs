using System.IO.Abstractions.TestingHelpers;
using Avalonia.Headless.XUnit;
using DAORework.App.ViewModels;
using DAORework.App.Views;
using DAORework.Core.Common;
using Xunit;

namespace DAORework.App.Tests;

/// <summary>The first-run flow (P2.5-07): the first profile is diagnosed at once, and the result is a short list of next steps.</summary>
public sealed class NextStepsScreenTests : IDisposable
{
    private readonly AppTestHost _host = new();
    private readonly byte[] _executable = FixFixtures.Executable();

    public void Dispose() => _host.Dispose();

    [AvaloniaFact]
    public async Task FirstProfile_IsDiagnosed_AndSuggestsThePatchDxvkAndAdopting()
    {
        var (window, viewModel) = await CreateFirstProfileAsync();

        Assert.True(viewModel.Diagnostics.HasReport);
        Assert.Equal(
            [NextStepKind.ApplyLargeAddressAware, NextStepKind.InstallDxvk, NextStepKind.AdoptHandInstalled],
            viewModel.NextSteps.Steps.Select(s => s.Kind));
        Assert.StartsWith("Manage 4 mods", viewModel.NextSteps.Steps[2].Title, StringComparison.Ordinal);
        Assert.Equal(MainWindowViewModel.HomePage, viewModel.SelectedPage);
        Screenshots.Save(window, "21-next-steps");
    }

    [AvaloniaFact]
    public async Task PatchStep_OpensThePlanOnTheFixesTab_AndChangesNothing()
    {
        var (_, viewModel) = await CreateFirstProfileAsync();

        await viewModel.NextSteps.TakeCommand.ExecuteAsync(viewModel.NextSteps.Steps.Single(s => s.Kind == NextStepKind.ApplyLargeAddressAware));

        Assert.Equal(MainWindowViewModel.FixesPage, viewModel.SelectedPage);
        Assert.Equal("Apply 4 GB patch", viewModel.Fixes.Change.PlanTitle);
        Assert.Equal(_executable, _host.FileSystem.File.ReadAllBytes(GamePaths.GameExecutable(AppTestHost.InstallDirectory)));
    }

    [AvaloniaFact]
    public async Task AdoptStep_ListsTheModsOnTheModsTab()
    {
        var (_, viewModel) = await CreateFirstProfileAsync();

        await viewModel.NextSteps.TakeCommand.ExecuteAsync(viewModel.NextSteps.Steps.Single(s => s.Kind == NextStepKind.AdoptHandInstalled));

        Assert.Equal(MainWindowViewModel.ModsPage, viewModel.SelectedPage);
        Assert.Equal(MainWindowViewModel.InstalledByHandSection, viewModel.ModsSection);
        Assert.Equal(4, viewModel.HandInstalled.Candidates.Count);
        Assert.Equal("Found in your game (4)", viewModel.HandInstalled.SectionHeader);
    }

    [AvaloniaFact]
    public async Task Steps_FollowTheReport_ADiagnosisWithoutTheFlagIssueOffersNoPatch()
    {
        _host.FileSystem.AddFile(GamePaths.GameExecutable(AppTestHost.InstallDirectory), new MockFileData(_executable));
        var (_, viewModel) = await CreateFirstProfileAsync(seed: false);

        await viewModel.NextSteps.ShowAsync("Default", [], TestContext.Current.CancellationToken);

        Assert.Single(viewModel.NextSteps.Steps);
        Assert.Equal(NextStepKind.InstallDxvk, viewModel.NextSteps.Steps[0].Kind);
        Assert.True(viewModel.NextSteps.HasContent);
    }

    /// <summary>A Steam install whose executable lacks the 4 GB flag, no DXVK, and the realistic install's mods placed by hand.</summary>
    private async Task<(MainWindow Window, MainWindowViewModel ViewModel)> CreateFirstProfileAsync(bool seed = true)
    {
        if (seed)
        {
            _host.SeedRealisticInstall();
            _host.FileSystem.AddFile(GamePaths.GameExecutable(AppTestHost.InstallDirectory), new MockFileData(_executable));
        }

        var viewModel = _host.Resolve<MainWindowViewModel>();
        var window = new MainWindow { DataContext = viewModel };
        window.Show();
        await viewModel.InitializeAsync(TestContext.Current.CancellationToken);
        viewModel.Profiles.NewProfileName = "Default";
        await viewModel.Profiles.CreateProfileCommand.ExecuteAsync(null);
        window.UpdateLayout();
        return (window, viewModel);
    }
}
