using System.ComponentModel;
using System.Diagnostics;
using DAORework.Application.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;

namespace DAORework.Infrastructure.Launching;

public sealed class ProcessLauncher : IProcessLauncher
{
    public Result<int> Start(ProcessStartRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var startInfo = new ProcessStartInfo
        {
            FileName = request.FileName,
            WorkingDirectory = request.WorkingDirectory ?? string.Empty,
            UseShellExecute = true,
        };

        // One entry per argument: the runtime quotes each one (SDD §8.6, audit C5).
        foreach (var argument in request.Arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        try
        {
            using var process = Process.Start(startInfo);

            return process is null
                ? Result.Failure<int>(new Error(DiagnosticCodes.ProcessStartFailed, $"The process could not be started: {request.FileName}"))
                : Result.Success(process.Id);
        }
        catch (Win32Exception exception)
        {
            return Result.Failure<int>(new Error(
                DiagnosticCodes.ProcessStartFailed,
                $"The process could not be started: {request.FileName}. {exception.Message}",
                "Check that the executable exists and that you have permission to run it."));
        }
    }
}
