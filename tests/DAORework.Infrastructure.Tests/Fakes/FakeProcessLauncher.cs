using DAORework.Application.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;

namespace DAORework.Infrastructure.Tests.Fakes;

internal sealed class FakeProcessLauncher : IProcessLauncher
{
    public ProcessStartRequest? LastRequest { get; private set; }

    public bool Fail { get; init; }

    public Result<int> Start(ProcessStartRequest request)
    {
        LastRequest = request;
        return Fail
            ? Result.Failure<int>(new Error(DiagnosticCodes.ProcessStartFailed, "could not start"))
            : Result.Success(1234);
    }
}
