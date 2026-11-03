using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace DAORework.Infrastructure.Safety;

/// <summary>The Windows Antimalware Scan Interface in <c>amsi.dll</c> (ADR-0018), loaded only from System32.</summary>
[SupportedOSPlatform("windows")]
[SuppressMessage(
    "Interoperability",
    "SYSLIB1054:Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time",
    Justification = "LibraryImport needs AllowUnsafeBlocks, which Infrastructure keeps off; without trimming or AOT it gains nothing (P2.5-35).")]
internal static class AmsiNativeMethods
{
    [DllImport("amsi.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    public static extern int AmsiInitialize(string appName, out nint amsiContext);

    [DllImport("amsi.dll", ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    public static extern void AmsiUninitialize(nint amsiContext);

    [DllImport("amsi.dll", ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    public static extern int AmsiOpenSession(nint amsiContext, out nint amsiSession);

    [DllImport("amsi.dll", ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    public static extern void AmsiCloseSession(nint amsiContext, nint amsiSession);

    [DllImport("amsi.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    public static extern int AmsiScanBuffer(nint amsiContext, byte[] buffer, uint length, string contentName, nint amsiSession, out int result);
}
