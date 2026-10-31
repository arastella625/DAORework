using DAORework.Application.Abstractions;

namespace DAORework.Infrastructure.Tests.Fakes;

/// <summary>An in-memory registry: values keyed by root, key path and value name; sub-keys are derived from the paths.</summary>
internal sealed class FakeRegistry : IRegistry
{
    private readonly Dictionary<(RegistryRoot Root, string Key, string Value), string> _values = new(new KeyComparer());

    public FakeRegistry Set(RegistryRoot root, string keyPath, string valueName, string value)
    {
        _values[(root, keyPath, valueName)] = value;
        return this;
    }

    public string? GetStringValue(RegistryRoot root, string keyPath, string valueName) =>
        _values.TryGetValue((root, keyPath, valueName), out var value) ? value : null;

    public IReadOnlyList<string> GetSubKeyNames(RegistryRoot root, string keyPath)
    {
        var prefix = keyPath.TrimEnd('\\') + "\\";
        return _values.Keys
            .Where(k => k.Root == root && k.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Select(k => k.Key[prefix.Length..].Split('\\')[0])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private sealed class KeyComparer : IEqualityComparer<(RegistryRoot Root, string Key, string Value)>
    {
        public bool Equals((RegistryRoot Root, string Key, string Value) x, (RegistryRoot Root, string Key, string Value) y) =>
            x.Root == y.Root &&
            string.Equals(x.Key, y.Key, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(x.Value, y.Value, StringComparison.OrdinalIgnoreCase);

        public int GetHashCode((RegistryRoot Root, string Key, string Value) obj) =>
            HashCode.Combine(obj.Root, obj.Key.ToUpperInvariant(), obj.Value.ToUpperInvariant());
    }
}
