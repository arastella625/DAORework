using DAORework.Application.Profiles;
using DAORework.Application.Tests.Fakes;
using DAORework.Core.Diagnostics;
using DAORework.Core.Profiles;
using Xunit;

namespace DAORework.Application.Tests.Profiles;

public sealed class GetProfileTests
{
    private readonly InMemoryProfileRepository _repository = new();

    [Fact]
    public async Task ExecuteAsync_Known_ReturnsTheProfile()
    {
        var stored = new RuntimeProfile { Name = "A" };
        _repository.Profiles["A"] = stored;

        var result = await new GetProfile(_repository).ExecuteAsync(new GetProfileRequest("A"));

        Assert.Same(stored, result.Value);
    }

    [Fact]
    public async Task ExecuteAsync_Unknown_FailsNotFoundWithInitHint()
    {
        var result = await new GetProfile(_repository).ExecuteAsync(new GetProfileRequest("Nope"));

        Assert.Equal(DiagnosticCodes.ProfileNotFound, result.Error!.Code);
        Assert.Equal("Run: init Nope", result.Error.Recommendation);
    }
}
