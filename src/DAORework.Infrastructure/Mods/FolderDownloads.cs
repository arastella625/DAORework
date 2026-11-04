using System.IO.Abstractions;
using System.IO.Compression;
using DAORework.Application.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Downloads;
using SharpCompress.Archives;
using SharpCompress.Readers;

namespace DAORework.Infrastructure.Mods;

/// <summary>
/// A downloads folder, top level only (ADR-0034 §2, §4). Reads names and sizes, and an archive's list of names, straight
/// from <see cref="ZipArchive"/> and SharpCompress rather than <see cref="ModSource"/>, so no file is hashed or extracted.
/// Never writes there. These are files nobody chose, so anything an archive reader throws on one means only that it
/// can't be listed.
/// </summary>
public sealed class FolderDownloads : IDownloadsFolder
{
    private readonly IFileSystem _fileSystem;

    public FolderDownloads(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public Result<IReadOnlyList<DownloadFile>> ListFinished(string folder)
    {
        if (!_fileSystem.Directory.Exists(folder))
        {
            return Result.Failure<IReadOnlyList<DownloadFile>>(new Error(DiagnosticCodes.DownloadsFolderMissing, $"The downloads folder {folder} isn't there.", "Choose the folder your browser saves to."));
        }

        try
        {
            return Result.Success<IReadOnlyList<DownloadFile>>([.. _fileSystem.DirectoryInfo.New(folder).EnumerateFiles("*", SearchOption.TopDirectoryOnly)
                .Where(f => ModDownloadRule.IsDownload(f.Name) && IsFinished(f))
                .Select(f => new DownloadFile(f.FullName, f.Length, f.CreationTimeUtc, f.LastWriteTimeUtc))]);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return Result.Failure<IReadOnlyList<DownloadFile>>(new Error(DiagnosticCodes.DownloadsUnreadable, $"The downloads folder couldn't be read: {exception.Message}"));
        }
    }

    public Result<IReadOnlyList<string>> ReadEntryNames(string path)
    {
        try
        {
            using var stream = _fileSystem.File.OpenRead(path);
            var names = _fileSystem.Path.GetExtension(path).Equals(".zip", StringComparison.OrdinalIgnoreCase) ? ZipNames(stream) : ArchiveNames(stream);
            return names.Count > FolderModLibrary.MaxFiles
                ? Unlistable("it holds more files than any mod DAORework accepts")
                : Result.Success<IReadOnlyList<string>>(names);
        }
#pragma warning disable CA1031 // A crafted file can make a parser throw anything, and one bad download must not stop the look.
        catch (Exception exception) when (exception is not OperationCanceledException)
#pragma warning restore CA1031
        {
            return Unlistable(exception.GetType().Name);
        }
    }

    private static Result<IReadOnlyList<string>> Unlistable(string reason) =>
        Result.Failure<IReadOnlyList<string>>(new Error(DiagnosticCodes.DownloadsUnreadable, $"An archive couldn't be listed: {reason}."));

    private static List<string> ZipNames(Stream stream)
    {
        using var zip = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
        return [.. zip.Entries.Where(e => e.Name.Length > 0).Select(e => e.FullName.Replace('\\', '/'))];
    }

    private static List<string> ArchiveNames(Stream stream)
    {
        using var archive = ArchiveFactory.OpenArchive(stream, new ReaderOptions { LeaveStreamOpen = true });
        return [.. archive.Entries.Where(e => !e.IsDirectory && e.Key is not null).Select(e => e.Key!.Replace('\\', '/'))];
    }

    private bool IsFinished(IFileInfo file) =>
        file.Length > 0
        && (file.Attributes & FileAttributes.ReparsePoint) == 0
        && !_fileSystem.File.Exists(file.FullName + ".part")
        && OpensForReading(file.FullName);

    private bool OpensForReading(string path)
    {
        try
        {
            using var stream = _fileSystem.File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return false; // Still being written. It is looked at again next time.
        }
    }
}
