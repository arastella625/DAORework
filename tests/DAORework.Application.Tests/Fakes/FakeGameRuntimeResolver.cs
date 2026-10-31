using DAORework.Application.Launching;
using DAORework.Core.Launching;

namespace DAORework.Application.Tests.Fakes;

internal sealed class FakeGameRuntimeResolver : IGameRuntimeResolver
{
    public Dictionary<RuntimeMode, IGameRuntime> Runtimes { get; } = [];

    public RuntimeMode? RequestedMode { get; private set; }

    public IGameRuntime? Resolve(RuntimeMode mode)
    {
        RequestedMode = mode;
        return Runtimes.GetValueOrDefault(mode);
    }
}
