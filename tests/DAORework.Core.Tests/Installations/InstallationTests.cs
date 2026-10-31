using DAORework.Core.Installations;
using Xunit;

namespace DAORework.Core.Tests.Installations;

public sealed class InstallationTests
{
    [Fact]
    public void IsValid_NoExecutablesRecorded_IsFalse()
    {
        var installation = new Installation { InstallDirectory = "x", Distribution = Distribution.Unknown };

        Assert.False(installation.IsValid);
        Assert.False(installation.HasValidGameExecutable);
        Assert.False(installation.HasValidLauncherExecutable);
    }

    [Fact]
    public void IsValid_OnlyLauncherRecorded_IsTrueWithoutGameExecutable()
    {
        var installation = new Installation { InstallDirectory = "x", LauncherExecutablePath = "x/launcher.exe", Distribution = Distribution.Gog };

        Assert.True(installation.IsValid);
        Assert.False(installation.HasValidGameExecutable);
        Assert.True(installation.HasValidLauncherExecutable);
    }
}
