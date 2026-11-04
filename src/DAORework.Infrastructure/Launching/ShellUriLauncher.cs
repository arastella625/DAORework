using System.ComponentModel;
using System.Diagnostics;
using DAORework.Application.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;

namespace DAORework.Infrastructure.Launching;

/// <summary>Opens a link through the platform's shell, as a double-click on a shortcut would (ADR-0022), which is <c>xdg-open</c> on Linux (ADR-0031).</summary>
public sealed class ShellUriLauncher : IUriLauncher
{
    public Result Open(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);

        try
        {
            using var process = Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
            return Result.Success();
        }
        catch (Win32Exception exception)
        {
            return Result.Failure(new Error(
                DiagnosticCodes.ProcessStartFailed,
                $"{uri.AbsoluteUri} could not be opened. {exception.Message}",
                uri.Scheme == Uri.UriSchemeHttps ? "Check that a default browser is set." : "Check that Steam is installed, or start the game from Steam."));
        }
    }
}
