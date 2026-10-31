using DAORework.Core.Common;
using DAORework.Core.Launching;
using DAORework.Core.Profiles;

namespace DAORework.Application.Launching;

/// <summary>One way of running the game (Strategy, SDD §5). Registered as a keyed service per <see cref="RuntimeMode"/>.</summary>
public interface IGameRuntime
{
    Task<Result<LaunchResult>> LaunchAsync(RuntimeProfile profile, CancellationToken cancellationToken = default);
}
