using System.Runtime.Versioning;
using DAORework.Application.Abstractions;
using Microsoft.Win32;

namespace DAORework.Infrastructure.Registry;

/// <summary>The <see cref="IRegistry"/> over <see cref="Microsoft.Win32.Registry"/>. Off Windows there is no registry, so nothing is found.</summary>
public sealed class WindowsRegistry : IRegistry
{
    public string? GetStringValue(RegistryRoot root, string keyPath, string valueName)
    {
        if (!OperatingSystem.IsWindows())
        {
            return null;
        }

        return ReadString(root, keyPath, valueName);
    }

    public IReadOnlyList<string> GetSubKeyNames(RegistryRoot root, string keyPath)
    {
        if (!OperatingSystem.IsWindows())
        {
            return [];
        }

        return ReadSubKeyNames(root, keyPath);
    }

    [SupportedOSPlatform("windows")]
    private static string? ReadString(RegistryRoot root, string keyPath, string valueName)
    {
        using var key = Hive(root).OpenSubKey(keyPath, writable: false);
        return key?.GetValue(valueName) as string;
    }

    [SupportedOSPlatform("windows")]
    private static string[] ReadSubKeyNames(RegistryRoot root, string keyPath)
    {
        using var key = Hive(root).OpenSubKey(keyPath, writable: false);
        return key?.GetSubKeyNames() ?? [];
    }

    [SupportedOSPlatform("windows")]
    private static RegistryKey Hive(RegistryRoot root) => root switch
    {
        RegistryRoot.CurrentUser => Microsoft.Win32.Registry.CurrentUser,
        _ => Microsoft.Win32.Registry.LocalMachine,
    };
}
