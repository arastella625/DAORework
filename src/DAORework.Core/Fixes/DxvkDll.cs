using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Formats;

namespace DAORework.Core.Fixes;

/// <summary>
/// Tells a DXVK <c>d3d9.dll</c> the game can load (ADR-0013): it carries the catalog's DXVK marker, whose text gives
/// the version, and it is a 32-bit build, since DXVK releases ship an x64 copy under the same name.
/// </summary>
public static class DxvkDll
{
    public const string UnknownVersion = "unknown";

    /// <summary>The DXVK version inside <paramref name="dll"/>, or why it is not a DXVK DLL the 32-bit game can load.</summary>
    public static Result<string> Identify(ReadOnlySpan<byte> dll, string marker)
    {
        if (!BinaryMarker.TryFind(dll, marker, out var version))
        {
            return Invalid("It is not a DXVK d3d9.dll (the DXVK version string is not in it).");
        }

        if (!PortableExecutableMachine.Read(dll).TryGetValue(out var machine, out _))
        {
            return Invalid("It is not a Windows DLL.");
        }

        return machine == PortableExecutableMachine.X86
            ? Result.Success(Version(version))
            : Invalid($"It is DXVK's {PortableExecutableMachine.Describe(machine)} build; the 32-bit game needs the one in the release's x32 folder.");
    }

    /// <summary>"v2.3" as the marker text reads, trimmed to "2.3"; a DLL with no text after the marker is of unknown version.</summary>
    private static string Version(string? text) =>
        string.IsNullOrWhiteSpace(text) ? UnknownVersion : text.Trim().TrimStart('v', 'V');

    private static Result<string> Invalid(string reason) =>
        Result.Failure<string>(new Error(DiagnosticCodes.FixSourceInvalid, $"The file supplied for DXVK cannot be used. {reason}"));
}
