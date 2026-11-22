using System.IO.Abstractions;
using System.Text;
using DAORework.Application.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Formats.Erf;

namespace DAORework.Infrastructure.Mods;

/// <summary>
/// Finds the game's own <c>chargenmorphcfg.xml</c> in the install's <c>packages/core/data</c>: a loose copy, else the
/// first archive whose table of contents names it (ADR-0025, amended 2026-11-22). Only tables of contents are read until
/// it is found, and only plain data: an archive that can't be read, or an encrypted or compressed copy, is passed over.
/// </summary>
public sealed class GameChargenMorphSource(IFileSystem fileSystem) : IGameChargenMorphSource
{
    public Task<Result<string?>> ReadAsync(string installDirectory, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(installDirectory);
        return Task.Run(() => Read(installDirectory, cancellationToken), cancellationToken);
    }

    private Result<string?> Read(string installDirectory, CancellationToken cancellationToken)
    {
        var data = GamePaths.CoreDataDirectory(installDirectory);
        try
        {
            if (!fileSystem.Directory.Exists(data))
            {
                return Result.Success<string?>(null);
            }

            var loose = fileSystem.Path.Combine(data, GamePaths.ChargenMorphFileName);
            if (fileSystem.File.Exists(loose))
            {
                return Result.Success<string?>(fileSystem.File.ReadAllText(loose));
            }

            foreach (var archive in fileSystem.Directory.EnumerateFiles(data, "*.erf").Order(StringComparer.OrdinalIgnoreCase))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (FromArchive(archive) is { } text)
                {
                    return Result.Success<string?>(text);
                }
            }

            return Result.Success<string?>(null);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return Result.Failure<string?>(new Error(
                DiagnosticCodes.GameChargenMorphUnreadable,
                $"The game's {GamePaths.ChargenMorphFileName} couldn't be read: {exception.Message}"));
        }
    }

    // An archive that isn't a readable ERF, or whose copy isn't plain data, can't be the one to count from.
    private string? FromArchive(string path)
    {
        using var stream = fileSystem.File.OpenRead(path);
        try
        {
            var table = ErfReader.ReadTableOfContents(stream);
            var entry = table.Entries.FirstOrDefault(e => e.Name.Equals(GamePaths.ChargenMorphFileName, StringComparison.OrdinalIgnoreCase));
            return entry is null ? null : Decode(ErfEntryReader.Read(stream, table, entry));
        }
        catch (InvalidDataException)
        {
            return null;
        }
    }

    private static string Decode(byte[] bytes)
    {
        using var reader = new StreamReader(new MemoryStream(bytes), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }
}
