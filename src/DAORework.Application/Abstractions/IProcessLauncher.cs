using DAORework.Core.Common;

namespace DAORework.Application.Abstractions;

/// <summary>Starts an external process (ADR-0003). Returns the process id.</summary>
public interface IProcessLauncher
{
    Result<int> Start(ProcessStartRequest request);
}
