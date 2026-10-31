using Avalonia.Headless.XUnit;
using DAORework.App.Views;
using DAORework.Application.Profiles;
using DAORework.Core.Diagnostics;
using Xunit;

namespace DAORework.App.Tests;

/// <summary>Activating and deactivating a library mod from the Mods tab (P3-04): the plan first, then Apply, as in the CLI.</summary>
public sealed class ModActivationScreenTests : IDisposable
{
    private readonly AppTestHost _host = new();
    private readonly ModsTabScenario _scenario;

    public ModActivationScreenTests()
    {
        _scenario = new ModsTabScenario(_host);
    }

    public void Dispose() => _host.Dispose();

    [AvaloniaFact]
    public async Task Activate_ProfileWithoutADocumentsFolder_SaysWhereToChooseOne()
    {
        var (_, viewModel) = await _scenario.OpenAsync(seedInstall: false);
        var profiles = _host.Resolve<IProfileRepository>();
        var profile = await profiles.LoadAsync("Default", TestContext.Current.CancellationToken);
        await profiles.SaveAsync(profile! with { UserDocumentsDirectory = null }, TestContext.Current.CancellationToken);
        await _scenario.ImportHairPackAsync(viewModel);

        await viewModel.ModsTab.Activation.ActivateCommand.ExecuteAsync(viewModel.Library.Mods[0]);

        Assert.True(viewModel.ModsTab.Activation.IsError);
        Assert.Contains("has no documents folder", viewModel.ModsTab.Activation.StatusMessage, StringComparison.Ordinal);
        Assert.Contains("Choose it on the Profile page", viewModel.ModsTab.Activation.StatusMessage, StringComparison.Ordinal);
        Assert.EndsWith($"({DiagnosticCodes.ModDocumentsNotSet})", viewModel.ModsTab.Activation.StatusMessage, StringComparison.Ordinal);
        Assert.Empty(viewModel.ModsTab.Activation.PlanLines);
    }

    [AvaloniaFact]
    public async Task Activate_ShowsThePlan_ThenApplyInstallsIt_AndDeactivateTakesItOut()
    {
        var (window, viewModel) = await _scenario.OpenAsync();
        await _scenario.ImportHairPackAsync(viewModel);
        var activation = viewModel.ModsTab.Activation;
        var mod = viewModel.Library.Mods[0];

        await activation.ActivateCommand.ExecuteAsync(mod);

        Assert.False(activation.IsError, activation.StatusMessage);
        Assert.Equal("Activate Hair Pack", activation.PlanTitle);
        Assert.Contains(activation.PlanLines, l => l == $"Mods: no mods -> 1 ({mod.Id})");
        Assert.Contains("  Copy      1 file", activation.PlanLines);
        Assert.True(activation.ApplyCommand.CanExecute(null));
        Assert.Empty(_scenario.DeployedHair());
        ModsTabScenario.Show<LibraryPanel>(window);
        Screenshots.Save(window, "14-activate-plan");

        await activation.ApplyCommand.ExecuteAsync(null);

        Assert.False(activation.IsError, activation.StatusMessage);
        Assert.StartsWith("Done: ", activation.StatusMessage, StringComparison.Ordinal);
        Assert.Contains("Backup ", activation.StatusMessage, StringComparison.Ordinal);
        Assert.Null(activation.PlanTitle);
        Assert.True(viewModel.Library.Mods[0].IsActive);
        Assert.Single(_scenario.DeployedHair());

        await activation.DeactivateCommand.ExecuteAsync(viewModel.Library.Mods[0]);
        Assert.Contains("  Delete    1 file", activation.PlanLines);
        await activation.ApplyCommand.ExecuteAsync(null);

        Assert.False(viewModel.Library.Mods[0].IsActive);
        Assert.Empty(_scenario.DeployedHair());
    }

    [AvaloniaFact]
    public async Task Cancel_LeavesEverythingAsItWas()
    {
        var (_, viewModel) = await _scenario.OpenAsync();
        await _scenario.ImportHairPackAsync(viewModel);
        var activation = viewModel.ModsTab.Activation;
        await activation.ActivateCommand.ExecuteAsync(viewModel.Library.Mods[0]);

        activation.CancelPlanCommand.Execute(null);

        Assert.Equal("Nothing was changed.", activation.StatusMessage);
        Assert.Empty(activation.PlanLines);
        Assert.False(activation.ApplyCommand.CanExecute(null));
        Assert.False(viewModel.Library.Mods[0].IsActive);
        Assert.Empty(_scenario.DeployedHair());
    }
}
