using System.Text.Json;
using DAORework.Core.Launching;
using DAORework.Core.Profiles;
using DAORework.Infrastructure.Persistence;
using Xunit;

namespace DAORework.Infrastructure.Tests.Persistence;

public sealed class JsonDefaultsTests
{
    [Fact]
    public void Serialize_UsesCamelCaseAndEnumNames()
    {
        var profile = new RuntimeProfile { Name = "X", LaunchTarget = LaunchTarget.OfficialLauncher };

        var json = JsonSerializer.Serialize(profile, JsonDefaults.Options);

        Assert.Contains("\"launchTarget\": \"OfficialLauncher\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"LaunchTarget\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Deserialize_AcceptsLegacyNumericEnums()
    {
        const string legacy = """{"name":"X","launchTarget":1,"runtimeMode":0}""";

        var profile = JsonSerializer.Deserialize<RuntimeProfile>(legacy, JsonDefaults.Options);

        Assert.NotNull(profile);
        Assert.Equal(LaunchTarget.OfficialLauncher, profile.LaunchTarget);
    }

    [Fact]
    public void Options_AreReadOnly()
    {
        Assert.True(JsonDefaults.Options.IsReadOnly);
    }
}
