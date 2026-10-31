using DAORework.Core.Launching;

namespace DAORework.Application.Launching;

/// <summary>Picks the <see cref="IGameRuntime"/> for a mode, or null when none is registered (Factory via keyed DI, SDD §5).</summary>
public interface IGameRuntimeResolver
{
    IGameRuntime? Resolve(RuntimeMode mode);
}
