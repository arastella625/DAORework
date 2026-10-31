using DAORework.Application.Launching;
using DAORework.Core.Common;
using DAORework.Core.Launching;
using DAORework.Core.Profiles;

namespace DAORework.Application.Tests.Fakes;

internal sealed class FakeGameRuntime : IGameRuntime
{
    public RuntimeProfile? LaunchedProfile { get; private set; }

    public Result<LaunchResult> Outcome { get; init; } = Result.Success(new LaunchResult(4242, "fake.exe"));

    public Task<Result<LaunchResult>> LaunchAsync(RuntimeProfile profile, CancellationToken cancellationToken = default)
    {
        LaunchedProfile = profile;
        return Task.FromResult(Outcome);
    }
}
