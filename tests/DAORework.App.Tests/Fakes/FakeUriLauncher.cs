using DAORework.Application.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;

namespace DAORework.App.Tests.Fakes;

/// <summary>Records the link instead of opening it, so no test starts Steam.</summary>
internal sealed class FakeUriLauncher : IUriLauncher
{
    public Uri? LastUri { get; private set; }

    public bool Fail { get; init; }

    public Result Open(Uri uri)
    {
        LastUri = uri;
        return Fail ? Result.Failure(new Error(DiagnosticCodes.ProcessStartFailed, "no handler for steam://")) : Result.Success();
    }
}
