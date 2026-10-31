using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace DAORework.Cli.Composition;

/// <summary>The only place outside the keyed-service resolver that touches the container (Standards §2).</summary>
public sealed class MicrosoftDependencyInjectionResolver : ITypeResolver, IDisposable
{
    private readonly ServiceProvider _provider;

    public MicrosoftDependencyInjectionResolver(ServiceProvider provider)
    {
        _provider = provider;
    }

    public object? Resolve(Type? type) => type is null ? null : _provider.GetService(type);

    public void Dispose() => _provider.Dispose();
}
