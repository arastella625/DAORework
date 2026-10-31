using System.Buffers.Binary;
using System.IO.Abstractions.TestingHelpers;
using DAORework.Cli.Composition;
using Xunit;

namespace DAORework.Cli.Tests;

/// <summary>fixes apply, remove, status and rollback end to end, over an install folder in memory holding a made-up exe.</summary>
public sealed class CliFixesTests : IDisposable
{
    private static readonly string _install = MockUnixSupport.Path(@"D:\Games\Dragon Age");
    private static readonly string _exe = Path.Combine(_install, "bin_ship", "daorigins.exe");
    private static readonly byte[] _original = Executable();

    private readonly CliTestHost _host = new();

    public CliFixesTests()
    {
        _host.FileSystem.AddFile(_exe, new MockFileData(_original));
        _host.Run("init", "Fixes");
        _host.Run("profile", "set-path", "Fixes", "install", _install);
    }

    public void Dispose() => _host.Dispose();

    [Fact]
    public void Status_BeforeAnything_SaysNoFixes()
    {
        var result = _host.Run("fixes", "status", "-p", "Fixes");

        Assert.Equal(ExitCodes.Success, result.ExitCode);
        Assert.Contains("No fixes are applied through DAORework.", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void Apply_DryRun_ShowsThePlanAndChangesNothing()
    {
        var result = _host.Run("fixes", "apply", "laa", "-p", "Fixes", "--dry-run");

        Assert.Equal(ExitCodes.Success, result.ExitCode);
        Assert.Contains("Apply: 4 GB patch (large-address-aware-", result.Output, StringComparison.Ordinal);
        Assert.Contains("  Replace bin_ship/daorigins.exe", result.Output, StringComparison.Ordinal);
        Assert.Contains("Dry run: nothing was changed.", result.Output, StringComparison.Ordinal);
        Assert.Equal(_original, Exe());
    }

    [Fact]
    public void Apply_WithoutYesWhenTheConsoleCannotAsk_ChangesNothing()
    {
        var result = _host.Run("fixes", "apply", "laa", "-p", "Fixes");

        Assert.Equal(ExitCodes.Failure, result.ExitCode);
        Assert.Equal(_original, Exe());
    }

    [Fact]
    public void ApplyThenRemove_SetsTheFlagAndThenPutsTheOriginalBack()
    {
        var applied = _host.Run("fixes", "apply", "laa", "-p", "Fixes", "--yes");

        Assert.Equal(ExitCodes.Success, applied.ExitCode);
        Assert.Contains("Undo with: fixes rollback", applied.Output, StringComparison.Ordinal);
        Assert.True(LargeAddressAware(Exe()));
        Assert.Contains(": in effect", _host.Run("fixes", "status", "-p", "Fixes").Output, StringComparison.Ordinal);
        Assert.Contains("Already in place", _host.Run("fixes", "apply", "4gb", "-p", "Fixes", "--yes").Output, StringComparison.Ordinal);

        var removed = _host.Run("fixes", "remove", "laa", "-p", "Fixes", "--yes");

        Assert.Equal(ExitCodes.Success, removed.ExitCode);
        Assert.Contains("  Put back the original bin_ship/daorigins.exe", removed.Output, StringComparison.Ordinal);
        Assert.Equal(_original, Exe());
        Assert.Contains("No fixes are applied", _host.Run("fixes", "status", "-p", "Fixes").Output, StringComparison.Ordinal);
    }

    [Fact]
    public void Rollback_UndoesTheApply()
    {
        _host.Run("fixes", "apply", "laa", "-p", "Fixes", "--yes");

        var result = _host.Run("fixes", "rollback", "-p", "Fixes", "--yes");

        Assert.Equal(ExitCodes.Success, result.ExitCode);
        Assert.Equal(_original, Exe());
    }

    [Fact]
    public void AfterSteamPutTheOriginalBack_StatusSaysSoAndRemoveOnlyForgetsIt()
    {
        _host.Run("fixes", "apply", "laa", "-p", "Fixes", "--yes");
        _host.FileSystem.File.WriteAllBytes(_exe, _original);

        var status = _host.Run("fixes", "status", "-p", "Fixes");
        var removed = _host.Run("fixes", "remove", "laa", "-p", "Fixes", "--yes");

        Assert.Contains("no longer in effect; changed outside DAORework since: bin_ship/daorigins.exe", status.Output, StringComparison.Ordinal);
        Assert.Contains("Nothing on disk needed changing", removed.Output, StringComparison.Ordinal);
        Assert.Equal(_original, Exe());
        Assert.Contains("No fixes are applied", _host.Run("fixes", "status", "-p", "Fixes").Output, StringComparison.Ordinal);
    }

    [Fact]
    public void Apply_WhileTheGameRuns_IsBlocked()
    {
        _host.FileSystem.AddFile(_exe, new MockFileData(_original) { AllowedFileShare = FileShare.Read });

        var result = _host.Run("fixes", "apply", "laa", "-p", "Fixes", "--yes");

        Assert.Equal(ExitCodes.Failure, result.ExitCode);
        Assert.Contains("in use, most likely by the running game", result.Output, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("fixes", "apply", "reshade", "-p", "Fixes", "--yes")]
    [InlineData("fixes", "remove", "laa", "-p", "Fixes", "--yes")]
    [InlineData("fixes", "recover", "-p", "Fixes", "--finish")]
    [InlineData("fixes", "rollback", "-p", "Fixes", "--yes")]
    [InlineData("fixes", "status", "-p", "NoSuchProfile")]
    public void Commands_WithNothingToActOn_Fail(params string[] args)
    {
        Assert.Equal(ExitCodes.Failure, _host.Run(args).ExitCode);
    }

    [Fact]
    public void ProfileWithoutAnInstallFolder_SaysHowToSetIt()
    {
        _host.Run("init", "Empty");

        var result = _host.Run("fixes", "status", "-p", "Empty");

        Assert.Equal(ExitCodes.Failure, result.ExitCode);
        Assert.Contains("Run: profile set-path Empty install <folder>", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void Recover_WithBothOrNeitherChoice_IsInvalidUsage()
    {
        Assert.Equal(ExitCodes.InvalidUsage, _host.Run("fixes", "recover", "-p", "Fixes").ExitCode);
    }

    /// <summary>A made-up 32-bit PE image: headers only, nothing of the game's.</summary>
    private static byte[] Executable()
    {
        var bytes = new byte[512];
        "MZ"u8.CopyTo(bytes);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(0x3C), 0x80);
        "PE\0\0"u8.CopyTo(bytes.AsSpan(0x80));
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(0x84), 0x014C);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(0x84 + 16), 224);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(0x84 + 18), 0x0102);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(0x98), 0x010B);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(0x98 + 92), 16);
        return bytes;
    }

    private static bool LargeAddressAware(byte[] exe) => (BinaryPrimitives.ReadUInt16LittleEndian(exe.AsSpan(0x84 + 18)) & 0x0020) != 0;

    private byte[] Exe() => _host.FileSystem.File.ReadAllBytes(_exe);
}
