using System.Buffers.Binary;
using System.IO.Abstractions.TestingHelpers;
using DAORework.Application.Diagnostics.Rules;
using DAORework.Application.Tests.Fakes;
using DAORework.Core.Diagnostics;
using DAORework.Core.Profiles;
using Xunit;

namespace DAORework.Application.Tests.Diagnostics.Rules;

public sealed class LargeAddressAwareRuleTests
{
    private static readonly string _exe = TestPaths.Root("Games", "DAO", "bin_ship", "daorigins.exe");

    private readonly MockFileSystem _fileSystem = new();

    [Fact]
    public async Task EvaluateAsync_NoExecutable_ReportsNothing()
    {
        Assert.Empty(await Evaluate());
    }

    [Theory]
    [InlineData(0x0122, "DAO_LAA_ENABLED", DiagnosticSeverity.Info)]
    [InlineData(0x0102, "DAO_LAA_DISABLED", DiagnosticSeverity.Warning)]
    public async Task EvaluateAsync_ReportsTheFlag(int characteristics, string code, DiagnosticSeverity severity)
    {
        _fileSystem.AddFile(_exe, new MockFileData(Header((ushort)characteristics)));

        var issue = Assert.Single(await Evaluate());

        Assert.Equal(code, issue.Code);
        Assert.Equal(severity, issue.Severity);
    }

    [Fact]
    public async Task EvaluateAsync_SteamProtectedExecutable_LeavesItToTheSteamRule()
    {
        _fileSystem.AddFile(_exe, new MockFileData(Fixes.TestExecutable.Build(steamProtected: true)));

        Assert.Empty(await Evaluate());
    }

    [Fact]
    public async Task EvaluateAsync_NotAnExecutable_WarnsUnreadable()
    {
        _fileSystem.AddFile(_exe, new MockFileData("not an exe"));

        var issue = Assert.Single(await Evaluate());

        Assert.Equal(DiagnosticCodes.LargeAddressAwareUnreadable, issue.Code);
        Assert.NotEqual(DiagnosticSeverity.Error, issue.Severity);
    }

    private Task<IReadOnlyList<DiagnosticIssue>> Evaluate() =>
        new LargeAddressAwareRule(_fileSystem).EvaluateAsync(new RuntimeProfile { GameExecutablePath = _exe });

    private static byte[] Header(ushort characteristics)
    {
        var bytes = new byte[512];
        bytes[0] = (byte)'M';
        bytes[1] = (byte)'Z';
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(0x3C), 0x80);
        "PE\0\0"u8.CopyTo(bytes.AsSpan(0x80));
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(0x80 + 22), characteristics);
        return bytes;
    }
}
