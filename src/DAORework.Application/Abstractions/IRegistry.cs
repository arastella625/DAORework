namespace DAORework.Application.Abstractions;

/// <summary>
/// Read-only access to the Windows registry (ADR-0003), the way Steam, GOG and EA App record where a game is.
/// Implemented in Infrastructure; faked in tests. On a platform without a registry every call finds nothing.
/// </summary>
public interface IRegistry
{
    /// <returns>The string value, or null when the key or value does not exist.</returns>
    string? GetStringValue(RegistryRoot root, string keyPath, string valueName);

    /// <returns>The names of the direct sub-keys, or an empty list when the key does not exist.</returns>
    IReadOnlyList<string> GetSubKeyNames(RegistryRoot root, string keyPath);
}
