using DAORework.Core.Diagnostics;
using DAORework.Core.Mods;
using DAORework.Core.Profiles;
using Xunit;

namespace DAORework.Core.Tests.Profiles;

public sealed class RuntimeProfileTests
{
    [Fact]
    public void NewDocuments_CarryTheCurrentSchemaVersion()
    {
        Assert.Equal(RuntimeProfile.CurrentSchemaVersion, new RuntimeProfile().SchemaVersion);
        Assert.Equal(DaoModProfile.CurrentSchemaVersion, new DaoModProfile().SchemaVersion);
        Assert.Equal(DiagnosticReport.CurrentSchemaVersion, new DiagnosticReport { CreatedAtUtc = DateTimeOffset.UnixEpoch }.SchemaVersion);
    }

    [Fact]
    public void With_KeepsTheSchemaVersion()
    {
        var profile = new RuntimeProfile { Name = "A" } with { LaunchTarget = Core.Launching.LaunchTarget.OfficialLauncher };

        Assert.Equal(RuntimeProfile.CurrentSchemaVersion, profile.SchemaVersion);
    }
}
