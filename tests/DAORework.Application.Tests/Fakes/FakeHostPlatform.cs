using DAORework.Application.Abstractions;

namespace DAORework.Application.Tests.Fakes;

internal sealed class FakeHostPlatform : IHostPlatform
{
    public bool IsLinux { get; init; }
}
