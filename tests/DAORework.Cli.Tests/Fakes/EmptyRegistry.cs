using DAORework.Application.Abstractions;

namespace DAORework.Cli.Tests.Fakes;

/// <summary>A registry with nothing in it, so the tests never read the machine they run on.</summary>
internal sealed class EmptyRegistry : IRegistry
{
    public string? GetStringValue(RegistryRoot root, string keyPath, string valueName) => null;

    public IReadOnlyList<string> GetSubKeyNames(RegistryRoot root, string keyPath) => [];
}
