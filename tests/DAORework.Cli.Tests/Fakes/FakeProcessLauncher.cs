using DAORework.Application.Abstractions;
using DAORework.Core.Common;

namespace DAORework.Cli.Tests.Fakes;

internal sealed class FakeProcessLauncher : IProcessLauncher
{
    public ProcessStartRequest? LastRequest { get; private set; }

    public Result<int> Start(ProcessStartRequest request)
    {
        LastRequest = request;
        return Result.Success(4242);
    }
}
