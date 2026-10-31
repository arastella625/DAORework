using DAORework.Application.Launching;
using DAORework.Core.Launching;
using Microsoft.Extensions.DependencyInjection;

namespace DAORework.Infrastructure.Launching;

/// <summary>Resolves the runtime registered under the mode as a keyed service. The only place outside the composition root that touches the container (Standards §2).</summary>
public sealed class KeyedGameRuntimeResolver : IGameRuntimeResolver
{
    private readonly IServiceProvider _services;

    public KeyedGameRuntimeResolver(IServiceProvider services)
    {
        _services = services;
    }

    public IGameRuntime? Resolve(RuntimeMode mode) => _services.GetKeyedService<IGameRuntime>(mode);
}
