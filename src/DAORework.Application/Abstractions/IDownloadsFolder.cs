using DAORework.Core.Common;
using DAORework.Core.Mods.Downloads;

namespace DAORework.Application.Abstractions;

/// <summary>Reads a downloads folder (ADR-0034 §9). It never writes, moves or deletes anything there.</summary>
public interface IDownloadsFolder
{
    /// <summary>The finished downloads at the folder's top level: not empty, not still being written, and not links.</summary>
    Result<IReadOnlyList<DownloadFile>> ListFinished(string folder);

    /// <summary>An archive's file names, and nothing else: nothing is extracted, hashed or scanned.</summary>
    Result<IReadOnlyList<string>> ReadEntryNames(string path);
}
