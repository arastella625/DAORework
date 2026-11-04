using DAORework.Application.Abstractions;
using DAORework.Application.Catalog;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using Xunit;

namespace DAORework.Application.Tests.Catalog;

/// <summary>Only <c>https</c> links open (ADR-0034 §7).</summary>
public sealed class OpenModPageTests
{
    private readonly RecordingLauncher _launcher = new();

    [Fact]
    public void Execute_AnHttpsLink_OpensIt()
    {
        Assert.True(new OpenModPage(_launcher).Execute("https://www.nexusmods.com/dragonage/mods/664").IsSuccess);

        Assert.Equal("https://www.nexusmods.com/dragonage/mods/664", _launcher.Opened?.AbsoluteUri);
    }

    [Theory]
    [InlineData("http://example.com/")]
    [InlineData("steam://rungameid/47810")]
    [InlineData("file:///C:/Windows/System32/calc.exe")]
    [InlineData(@"C:\Windows\System32\calc.exe")]
    [InlineData("calc.exe")]
    [InlineData("")]
    [InlineData(null)]
    public void Execute_AnythingElse_IsRefusedAndNothingOpens(string? link)
    {
        var result = new OpenModPage(_launcher).Execute(link);

        Assert.Equal(DiagnosticCodes.ModPageLinkRefused, result.Error!.Code);
        Assert.Null(_launcher.Opened);
    }

    private sealed class RecordingLauncher : IUriLauncher
    {
        public Uri? Opened { get; private set; }

        public Result Open(Uri uri)
        {
            Opened = uri;
            return Result.Success();
        }
    }
}
