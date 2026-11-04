using System.IO.Abstractions.TestingHelpers;
using DAORework.Core.Common;
using Xunit;

namespace DAORework.Cli.Tests;

/// <summary>Finding a documents folder that is not where Windows says (ADR-0032), through the real composition.</summary>
public sealed class CliDocumentsTests : IDisposable
{
    private readonly CliTestHost _host = new();

    public void Dispose() => _host.Dispose();

    [Fact]
    public void Detect_FolderAProfileNames_ListsItFromTheProfile()
    {
        var moved = GamePaths.DefaultDocumentsDirectory(Path.Combine(Path.GetDirectoryName(_host.KnownFolders.UserProfile)!, "Games"));
        _host.Run("init", "Smoke");
        _host.FileSystem.AddDirectory(moved);
        _host.Run("profile", "set-path", "Smoke", "documents", moved);

        var result = _host.Run("detect");

        Assert.Contains($"[Profile] {moved}  (exists)", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void Detect_FolderElsewhere_SaysWhatANewProfileUsesAndNotWhereTheGameLooks()
    {
        var moved = GamePaths.DefaultDocumentsDirectory(Path.Combine(Path.GetDirectoryName(_host.KnownFolders.UserProfile)!, "Games"));
        _host.Run("init", "Smoke");
        _host.FileSystem.AddDirectory(moved);
        _host.Run("profile", "set-path", "Smoke", "documents", moved);

        var result = _host.Run("detect");

        Assert.Contains("A new profile uses the folder listed first, not the one Windows reports.", result.Output, StringComparison.Ordinal);
        Assert.DoesNotContain("The game currently uses", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void DetectAndInit_FolderOnlyOnADrive_FindsItAndTheProfileKeepsIt()
    {
        _host.FileSystem.AddDrive("C:", new MockDriveData { DriveType = DriveType.Fixed });

        // Read back, not assumed: the mock's root for a drive differs by OS (C:\ on Windows, /C: elsewhere).
        var root = _host.FileSystem.DriveInfo.GetDrives().Single(d => d.DriveType == DriveType.Fixed).RootDirectory.FullName;
        var moved = GamePaths.DefaultDocumentsDirectory(root);
        _host.FileSystem.AddDirectory(moved);

        var detected = _host.Run("detect");
        _host.Run("init", "Smoke");
        var again = _host.Run("detect");

        Assert.Contains($"[DriveSearch] {moved}  (exists)", detected.Output, StringComparison.Ordinal);
        Assert.Contains($"[Profile] {moved}  (exists)", again.Output, StringComparison.Ordinal);
    }
}
