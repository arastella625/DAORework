using DAORework.App.ViewModels;
using DAORework.Core.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DAORework.App.Tests;

/// <summary>A success message fades; how things stand, and an error, stay (docs/VOICE.md rule 4).</summary>
public sealed class StatusFadeTests
{
    [Fact]
    public async Task Success_Fades()
    {
        var page = new Page();

        page.Succeed("Turned on Blue Hair.");
        Assert.Equal("Turned on Blue Hair.", page.StatusMessage);
        await Task.Delay(300, TestContext.Current.CancellationToken);

        Assert.Equal(string.Empty, page.StatusMessage);
    }

    [Fact]
    public async Task ANewerMessage_IsNotClearedByAnOlderFade()
    {
        var page = new Page();

        page.Succeed("Turned on Blue Hair.");
        await Task.Delay(30, TestContext.Current.CancellationToken);
        page.Fail();
        await Task.Delay(300, TestContext.Current.CancellationToken);

        Assert.True(page.IsError);
        Assert.Contains("DAO_LOOK_NOT_FOUND", page.StatusMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Status_Stays()
    {
        var page = new Page();

        page.State("5 mods found in your game.");
        await Task.Delay(300, TestContext.Current.CancellationToken);

        Assert.Equal("5 mods found in your game.", page.StatusMessage);
    }

    private sealed class Page : PageViewModel
    {
        public Page()
            : base(NullLogger.Instance)
        {
            SuccessFadesAfter = TimeSpan.FromMilliseconds(100);
        }

        public void Succeed(string message) => ReportSuccess(message);

        public void State(string message) => ReportStatus(message);

        public void Fail() => ReportError(new Error("DAO_LOOK_NOT_FOUND", "Gone."));
    }
}
