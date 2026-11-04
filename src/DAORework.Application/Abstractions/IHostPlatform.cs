namespace DAORework.Application.Abstractions;

/// <summary>What DAORework is running on, for advice that differs by platform (ADR-0031). Implemented in Infrastructure; faked in tests.</summary>
public interface IHostPlatform
{
    bool IsLinux { get; }
}
