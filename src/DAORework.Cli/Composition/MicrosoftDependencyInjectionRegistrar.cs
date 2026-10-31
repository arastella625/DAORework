using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace DAORework.Cli.Composition;

/// <summary>Lets Spectre.Console.Cli construct commands from the application's service collection (ADR-0005).</summary>
public sealed class MicrosoftDependencyInjectionRegistrar : ITypeRegistrar
{
    private readonly IServiceCollection _services;

    public MicrosoftDependencyInjectionRegistrar(IServiceCollection services)
    {
        _services = services;
    }

    public ITypeResolver Build() => new MicrosoftDependencyInjectionResolver(_services.BuildServiceProvider());

    public void Register(Type service, Type implementation) => _services.AddSingleton(service, implementation);

    public void RegisterInstance(Type service, object implementation) => _services.AddSingleton(service, implementation);

    public void RegisterLazy(Type service, Func<object> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _services.AddSingleton(service, _ => factory());
    }
}
