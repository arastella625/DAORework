using System.Runtime.Versioning;

namespace DAORework.Infrastructure.Safety;

/// <summary>One AMSI context and session for one mod's files, closed when disposed.</summary>
[SupportedOSPlatform("windows")]
internal sealed class AmsiSession : IDisposable
{
    private readonly nint _context;
    private readonly nint _session;

    private AmsiSession(nint context, nint session)
    {
        _context = context;
        _session = session;
    }

    /// <summary>Opens a session, or returns null when AMSI is missing or refuses (no antivirus to ask).</summary>
    public static AmsiSession? TryOpen()
    {
        try
        {
            if (AmsiNativeMethods.AmsiInitialize("DAORework", out var context) != 0)
            {
                return null;
            }

            if (AmsiNativeMethods.AmsiOpenSession(context, out var session) != 0)
            {
                AmsiNativeMethods.AmsiUninitialize(context);
                return null;
            }

            return new AmsiSession(context, session);
        }
        catch (Exception exception) when (exception is DllNotFoundException or EntryPointNotFoundException)
        {
            return null;
        }
    }

    /// <summary>The AMSI result for one buffer, or null when the scan itself failed.</summary>
    public int? Scan(byte[] content, string name) =>
        AmsiNativeMethods.AmsiScanBuffer(_context, content, (uint)content.Length, name, _session, out var result) == 0 ? result : null;

    public void Dispose()
    {
        AmsiNativeMethods.AmsiCloseSession(_context, _session);
        AmsiNativeMethods.AmsiUninitialize(_context);
    }
}
