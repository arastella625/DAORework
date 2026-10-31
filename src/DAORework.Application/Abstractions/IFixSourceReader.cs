using DAORework.Core.Common;

namespace DAORework.Application.Abstractions;

/// <summary>Reads the file a fix needs out of what the user supplied (ADR-0013). Only reads; nothing is extracted to disk.</summary>
public interface IFixSourceReader
{
    /// <summary>
    /// The 32-bit <c>d3d9.dll</c> from a DXVK release as downloaded (<c>.tar.gz</c>), a <c>.zip</c>, an extracted folder
    /// (its <c>x32/d3d9.dll</c>), or the DLL itself. <c>DAO_FIX_SOURCE_INVALID</c> when there is none.
    /// </summary>
    Task<Result<byte[]>> ReadDxvkDllAsync(string sourcePath, CancellationToken cancellationToken = default);
}
