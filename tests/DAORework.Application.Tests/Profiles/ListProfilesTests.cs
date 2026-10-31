using DAORework.Application.Profiles;
using DAORework.Application.Tests.Fakes;
using DAORework.Core.Profiles;
using Xunit;

namespace DAORework.Application.Tests.Profiles;

public sealed class ListProfilesTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsNamesSortedIgnoringCase()
    {
        var repository = new InMemoryProfileRepository();
        foreach (var name in new[] { "beta", "Alpha", "gamma" })
        {
            repository.Profiles[name] = new RuntimeProfile { Name = name };
        }

        var result = await new ListProfiles(repository).ExecuteAsync(new ListProfilesRequest());

        Assert.Equal(["Alpha", "beta", "gamma"], result.Value);
    }

    [Fact]
    public async Task ExecuteAsync_Empty_ReturnsEmptyList()
    {
        var result = await new ListProfiles(new InMemoryProfileRepository()).ExecuteAsync(new ListProfilesRequest());

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }
}
