using System.IO.Abstractions.TestingHelpers;
using DAORework.Cli.Composition;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using Xunit;

namespace DAORework.Cli.Tests;

public sealed class CliCommandTests : IDisposable
{
    private static readonly string _install = MockUnixSupport.Path(@"C:\Games\DAO");

    private readonly CliTestHost _host = new();

    public void Dispose() => _host.Dispose();

    [Fact]
    public void Help_ListsEveryVerbAndExitsZero()
    {
        var result = _host.Run("--help");

        Assert.Equal(ExitCodes.Success, result.ExitCode);
        foreach (var verb in new[] { "detect", "init", "profiles", "profile", "diagnose", "launch", "mods" })
        {
            Assert.Contains(verb, result.Output, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void UnknownCommand_ExitsInvalidUsage()
    {
        var result = _host.Run("frobnicate");

        Assert.Equal(ExitCodes.InvalidUsage, result.ExitCode);
        Assert.Contains("frobnicate", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void Init_InvalidName_ExitsFailureWithReason()
    {
        var result = _host.Run("init", "bad:name");

        Assert.Equal(ExitCodes.Failure, result.ExitCode);
        Assert.Contains("Invalid profile name", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void Init_ThenProfilesAndShow_RoundTrip()
    {
        Assert.Equal(ExitCodes.Success, _host.Run("init", "Smoke").ExitCode);

        var list = _host.Run("profiles");
        var alias = _host.Run("profile", "list");
        var show = _host.Run("profile", "show", "Smoke");

        Assert.Equal(ExitCodes.Success, list.ExitCode);
        Assert.Equal("Smoke", list.Output.Trim());
        Assert.Equal(list.Output, alias.Output);
        Assert.Contains("Profile:           Smoke", show.Output, StringComparison.Ordinal);
        Assert.Contains(GamePaths.DefaultDocumentsDirectory(_host.KnownFolders.MyDocuments), show.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void ProfileShow_Unknown_ExitsFailureWithInitHint()
    {
        var result = _host.Run("profile", "show", "Nope");

        Assert.Equal(ExitCodes.Failure, result.ExitCode);
        Assert.Contains("Profile 'Nope' was not found.", result.Output, StringComparison.Ordinal);
        Assert.Contains("Run: init Nope", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void Detect_ManualPathWithTheGame_ListsItAsManual()
    {
        _host.FileSystem.AddFile(GamePaths.GameExecutable(_install), new MockFileData("exe"));

        var result = _host.Run("detect", "--path", _install);

        Assert.Equal(ExitCodes.Success, result.ExitCode);
        Assert.Contains($"[Manual] {_install}", result.Output, StringComparison.Ordinal);
        Assert.Contains($"Game:     {GamePaths.GameExecutable(_install)}", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void Detect_NothingAnywhere_SaysSoAndExitsZero()
    {
        var result = _host.Run("detect");

        Assert.Equal(ExitCodes.Success, result.ExitCode);
        Assert.Contains("No Dragon Age: Origins installation was detected", result.Output, StringComparison.Ordinal);
        Assert.Contains($"[KnownFolder] {GamePaths.DefaultDocumentsDirectory(_host.KnownFolders.MyDocuments)}  (not found)", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void Detect_DocumentsFolderExists_ListsItAsExisting()
    {
        var documents = GamePaths.DefaultDocumentsDirectory(_host.KnownFolders.MyDocuments);
        _host.FileSystem.AddDirectory(documents);

        var result = _host.Run("detect");

        Assert.Contains($"[KnownFolder] {documents}  (exists)", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void SupportBundle_UnknownProfile_ExitsFailure()
    {
        var result = _host.Run("support-bundle", "Nope");

        Assert.Equal(ExitCodes.Failure, result.ExitCode);
        Assert.Contains("Profile 'Nope' was not found.", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void SupportBundle_KnownProfile_WritesAZip()
    {
        _host.Run("init", "Smoke");

        var result = _host.Run("support-bundle", "Smoke");

        Assert.Equal(ExitCodes.Success, result.ExitCode);
        var path = result.Output.Split('\n').Single(l => l.StartsWith("Support bundle written to:", StringComparison.Ordinal))["Support bundle written to:".Length..].Trim();
        Assert.True(_host.FileSystem.File.Exists(path), path);
        Assert.EndsWith(".zip", path, StringComparison.Ordinal);
    }

    [Fact]
    public void SavesBackup_NoSaves_ExitsFailureWithTheReason()
    {
        _host.Run("init", "Smoke");

        var result = _host.Run("saves", "backup", "Smoke");

        Assert.Equal(ExitCodes.Failure, result.ExitCode);
        Assert.Contains("There are no saves to back up", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void SavesBackup_ThenList_ShowsTheBackupAndRetentionApplies()
    {
        _host.Run("init", "Smoke");
        var characters = GamePaths.CharactersDirectory(GamePaths.DefaultDocumentsDirectory(_host.KnownFolders.MyDocuments));
        _host.FileSystem.AddFile(Path.Combine(characters, "Aeducan", "Saves", "Slot_1", "Slot_1.das"), new MockFileData("save"));

        var first = _host.Run("saves", "backup", "Smoke", "--keep", "1");
        var second = _host.Run("saves", "backup", "Smoke", "--keep", "1");
        var list = _host.Run("saves", "list", "Smoke");

        Assert.Equal(ExitCodes.Success, first.ExitCode);
        Assert.Contains("Backed up 1 file(s)", first.Output, StringComparison.Ordinal);
        Assert.Contains("Removed old backup:", second.Output, StringComparison.Ordinal);
        Assert.Single(list.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries), l => l.Contains(".zip", StringComparison.Ordinal));
    }

    [Fact]
    public void SavesBackup_KeepZero_ExitsFailure()
    {
        _host.Run("init", "Smoke");

        var result = _host.Run("saves", "backup", "Smoke", "--keep", "0");

        Assert.Equal(ExitCodes.Failure, result.ExitCode);
        Assert.Contains("Keep between 1 and 1000", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void ProfileSetTarget_InvalidValue_ExitsInvalidUsage()
    {
        _host.Run("init", "Smoke");

        var result = _host.Run("profile", "set-target", "Smoke", "Sideways");

        Assert.Equal(ExitCodes.InvalidUsage, result.ExitCode);
        Assert.Contains("Invalid launch target", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void ProfileSetPath_UnknownKind_ExitsInvalidUsage()
    {
        _host.Run("init", "Smoke");

        var result = _host.Run("profile", "set-path", "Smoke", "bogus", "/tmp");

        Assert.Equal(ExitCodes.InvalidUsage, result.ExitCode);
        Assert.Contains("Invalid path type: bogus", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void ProfileSetPathAndTarget_ThenShow_ReflectsBoth()
    {
        _host.Run("init", "Smoke");
        var exe = MockUnixSupport.Path(@"C:\x y\bin_ship\daorigins.exe");

        Assert.Equal(ExitCodes.Success, _host.Run("profile", "set-path", "Smoke", "game", exe).ExitCode);
        Assert.Equal(ExitCodes.Success, _host.Run("profile", "set-target", "Smoke", "officiallauncher").ExitCode);
        var show = _host.Run("profile", "show", "Smoke");

        Assert.Contains($"Game Executable:   {exe}", show.Output, StringComparison.Ordinal);
        Assert.Contains("Launch Target:     OfficialLauncher", show.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void Diagnose_ProfileWithoutExecutable_PrintsCodesAndWritesReport()
    {
        _host.Run("init", "Smoke");

        var result = _host.Run("diagnose", "Smoke");

        Assert.Equal(ExitCodes.Success, result.ExitCode);
        Assert.Contains($"[ERROR] {DiagnosticCodes.GameExecutableMissing}", result.Output, StringComparison.Ordinal);
        Assert.Contains("Report written to:", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void Launch_ProfileFailsValidation_ExitsFailureWithReportPath()
    {
        _host.Run("init", "Smoke");

        var result = _host.Run("launch", "Smoke");

        Assert.Equal(ExitCodes.Failure, result.ExitCode);
        Assert.Contains("Launch failed: Profile 'Smoke' failed launch validation.", result.Output, StringComparison.Ordinal);
        Assert.Contains("Review the diagnostic report:", result.Output, StringComparison.Ordinal);
        Assert.Null(_host.ProcessLauncher.LastRequest);
    }

    [Fact]
    public void Launch_ValidProfile_StartsTheProcessAndPrintsItsId()
    {
        var exe = GamePaths.GameExecutable(_install);
        _host.FileSystem.AddFile(exe, new MockFileData("exe"));
        _host.FileSystem.AddDirectory(_install);
        _host.Run("init", "Smoke");
        _host.Run("profile", "set-path", "Smoke", "install", _install);
        _host.Run("profile", "set-path", "Smoke", "game", exe);

        var result = _host.Run("launch", "Smoke");

        Assert.Equal(ExitCodes.Success, result.ExitCode);
        Assert.Contains("Launch succeeded. ProcessId: 4242.", result.Output, StringComparison.Ordinal);
        Assert.Equal(exe, _host.ProcessLauncher.LastRequest?.FileName);
    }

    [Fact]
    public void Launch_SteamInstall_AsksSteamToStartTheGame()
    {
        var library = MockUnixSupport.Path(@"D:\SteamLibrary");
        var install = Path.Combine(library, "steamapps", "common", "Dragon Age Ultimate Edition");
        var exe = GamePaths.GameExecutable(install);
        _host.FileSystem.AddFile(exe, new MockFileData("exe"));
        _host.FileSystem.AddFile(
            Path.Combine(library, "steamapps", "appmanifest_47810.acf"),
            new MockFileData("\"AppState\" { \"appid\" \"47810\" \"installdir\" \"Dragon Age Ultimate Edition\" }"));
        _host.Run("init", "Steam");
        _host.Run("profile", "set-path", "Steam", "install", install);
        _host.Run("profile", "set-path", "Steam", "game", exe);

        var result = _host.Run("launch", "Steam");

        Assert.Equal(ExitCodes.Success, result.ExitCode);
        Assert.Contains("Launch succeeded. Steam was asked to start the game (steam://rungameid/47810).", result.Output, StringComparison.Ordinal);
        Assert.Equal("steam://rungameid/47810", _host.UriLauncher.LastUri?.AbsoluteUri);
        Assert.Null(_host.ProcessLauncher.LastRequest);
    }

    [Fact]
    public void ModsInspectAndInventory_WithFilesInOverride_ReportThem()
    {
        var documents = MockUnixSupport.Path(@"C:\Docs");
        var overrideDir = GamePaths.OverrideDirectory(documents);
        _host.FileSystem.AddFile(Path.Combine(overrideDir, "a.gda"), new MockFileData("hi"));
        _host.FileSystem.AddFile(Path.Combine(overrideDir, "sub", "a.gda"), new MockFileData("hi"));
        _host.Run("init", "Smoke");
        _host.Run("profile", "set-path", "Smoke", "documents", documents);

        var inspect = _host.Run("mods", "inspect", "Smoke");
        var inventory = _host.Run("mods", "inventory", "Smoke", "--hash");

        Assert.Equal(ExitCodes.Success, inspect.ExitCode);
        Assert.Contains("Files:   2", inspect.Output, StringComparison.Ordinal);
        Assert.Equal(ExitCodes.Success, inventory.ExitCode);
        Assert.Contains("Total Files:         2", inventory.Output, StringComparison.Ordinal);
        Assert.Contains("Duplicate Names:     1", inventory.Output, StringComparison.Ordinal);
        Assert.Contains("Inventory written to:", inventory.Output, StringComparison.Ordinal);
    }
}
