using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DAORework.Application.Abstractions;
using DAORework.Application.Mods;
using DAORework.Application.Profiles;
using DAORework.Core.Mods;
using DAORework.Core.Profiles;
using Microsoft.Extensions.Logging;

namespace DAORework.App.ViewModels;

/// <summary>The mods page: a summary of the override and AddIns folders, and the inventory written to disk as in the CLI.</summary>
public sealed partial class ModsPageViewModel : PageViewModel
{
    private readonly GetProfile _getProfile;
    private readonly InspectModFolders _inspectModFolders;
    private readonly BuildModInventory _buildModInventory;
    private readonly IModInventoryWriter _inventoryWriter;

    public ModsPageViewModel(
        GetProfile getProfile,
        InspectModFolders inspectModFolders,
        BuildModInventory buildModInventory,
        IModInventoryWriter inventoryWriter,
        ILogger<ModsPageViewModel> logger)
        : base(logger)
    {
        _getProfile = getProfile;
        _inspectModFolders = inspectModFolders;
        _buildModInventory = buildModInventory;
        _inventoryWriter = inventoryWriter;
    }

    public ObservableCollection<ModFolderSummary> Folders { get; } = [];

    [ObservableProperty]
    public partial string? ProfileName { get; set; }

    [ObservableProperty]
    public partial string? DocumentsDirectory { get; set; }

    [ObservableProperty]
    public partial bool IncludeHashes { get; set; }

    [ObservableProperty]
    public partial ModInventory? Inventory { get; set; }

    [ObservableProperty]
    public partial string? InventoryPath { get; set; }

    public void Load(string? profileName)
    {
        ProfileName = profileName;
        Folders.Clear();
        DocumentsDirectory = null;
        Inventory = null;
        InventoryPath = null;
        ReportSuccess(string.Empty);
    }

    [RelayCommand]
    private Task InspectAsync(CancellationToken cancellationToken) =>
        GuardAsync(
            async token =>
            {
                var profile = await LoadProfileAsync(token);
                if (profile is null)
                {
                    return;
                }

                var result = await _inspectModFolders.ExecuteAsync(new InspectModFoldersRequest(profile), token);
                if (!result.TryGetValue(out var report, out var error))
                {
                    ReportError(error);
                    return;
                }

                DocumentsDirectory = report.UserDocumentsDirectory;
                Folders.Clear();
                Folders.Add(report.OverrideFolder);
                Folders.Add(report.AddInsFolder);
                ReportStatus($"Inspected the mod folders under {report.UserDocumentsDirectory}.");
            },
            cancellationToken);

    [RelayCommand]
    private Task WriteInventoryAsync(CancellationToken cancellationToken) =>
        GuardAsync(
            async token =>
            {
                var profile = await LoadProfileAsync(token);
                if (profile is null)
                {
                    return;
                }

                var result = await _buildModInventory.ExecuteAsync(new BuildModInventoryRequest(profile, IncludeHashes), token);
                if (!result.TryGetValue(out var inventory, out var error))
                {
                    ReportError(error);
                    return;
                }

                InventoryPath = await _inventoryWriter.WriteAsync(inventory, token);
                Inventory = inventory;
                ReportStatus($"Inventory written to {InventoryPath}.");
            },
            cancellationToken);

    private async Task<RuntimeProfile?> LoadProfileAsync(CancellationToken cancellationToken)
    {
        if (ProfileName is null)
        {
            return null;
        }

        var result = await _getProfile.ExecuteAsync(new GetProfileRequest(ProfileName), cancellationToken);
        if (result.TryGetValue(out var profile, out var error))
        {
            return profile;
        }

        ReportError(error);
        return null;
    }
}
