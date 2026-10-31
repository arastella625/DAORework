using DAORework.Core.Diagnostics;
using DAORework.Core.Profiles;
using DAORework.Infrastructure.Launching;
using Xunit;

namespace DAORework.Infrastructure.Tests.Launching;

public sealed class ExperimentalRuntimeTests
{
    [Fact]
    public async Task LaunchAsync_AlwaysFailsAsUnavailable()
    {
        var result = await new ExperimentalRuntime().LaunchAsync(new RuntimeProfile());

        Assert.True(result.IsFailure);
        Assert.Equal(DiagnosticCodes.RuntimeExperimentalUnavailable, result.Error!.Code);
    }
}
