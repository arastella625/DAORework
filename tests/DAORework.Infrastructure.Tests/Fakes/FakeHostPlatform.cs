using DAORework.Application.Abstractions;

namespace DAORework.Infrastructure.Tests.Fakes;

internal sealed class FakeHostPlatform : IHostPlatform
{
    public bool IsLinux { get; init; }
}
