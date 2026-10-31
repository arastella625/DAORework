using DAORework.Application.Launching;
using DAORework.Core.Launching;
using DAORework.Infrastructure.Launching;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DAORework.Infrastructure.Tests.Launching;

public sealed class KeyedGameRuntimeResolverTests
{
    [Fact]
    public void Resolve_RegisteredMode_ReturnsThatRuntime()
    {
        var services = new ServiceCollection()
            .AddKeyedSingleton<IGameRuntime, ExperimentalRuntime>(RuntimeMode.ExperimentalRuntime)
            .BuildServiceProvider();

        var runtime = new KeyedGameRuntimeResolver(services).Resolve(RuntimeMode.ExperimentalRuntime);

        Assert.IsType<ExperimentalRuntime>(runtime);
    }

    [Fact]
    public void Resolve_UnregisteredMode_ReturnsNull()
    {
        var services = new ServiceCollection().BuildServiceProvider();

        Assert.Null(new KeyedGameRuntimeResolver(services).Resolve(RuntimeMode.OriginalGame));
    }
}
