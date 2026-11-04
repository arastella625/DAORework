using DAORework.Application.Abstractions;

namespace DAORework.Infrastructure.KnownFolders;

/// <summary>The platform .NET reports.</summary>
public sealed class EnvironmentHostPlatform : IHostPlatform
{
    public bool IsLinux => OperatingSystem.IsLinux();
}
