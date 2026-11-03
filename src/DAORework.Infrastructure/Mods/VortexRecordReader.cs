using System.IO.Abstractions;
using System.Text.Json;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Adoption;

namespace DAORework.Infrastructure.Mods;

/// <summary>
/// Reads one of Vortex's deployment records (ADR-0028 §1): each placed file's <c>relPath</c> and <c>source</c>, whether
/// the record still sits in the folder its <c>targetPath</c> names, and which mods the staging folder its
/// <c>stagingPath</c> names still holds, on whatever drive that is. Never writes, renames or deletes anything of Vortex's.
/// </summary>
public sealed class VortexRecordReader
{
    private readonly IFileSystem _fileSystem;

    public VortexRecordReader(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public async Task<Result<VortexRecord>> ReadAsync(string documentsDirectory, string recordPath, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var stream = _fileSystem.File.OpenRead(recordPath);
            using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var root = json.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("files", out var files) || files.ValueKind != JsonValueKind.Array)
            {
                return Unreadable(recordPath, "it has no list of files");
            }

            var folder = _fileSystem.Path.GetDirectoryName(_fileSystem.Path.GetFullPath(recordPath))!;
            var relativeFolder = Normal(_fileSystem.Path.GetRelativePath(_fileSystem.Path.GetFullPath(documentsDirectory), folder));
            return Result.Success(new VortexRecord(Placed(files, relativeFolder))
            {
                AtItsTarget = Normal(Text(root, "targetPath")).Equals(Normal(folder), StringComparison.OrdinalIgnoreCase),
                StagedMods = Staged(Text(root, "stagingPath")),
            });
        }
        catch (JsonException exception)
        {
            return Unreadable(recordPath, exception.Message);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return Unreadable(recordPath, exception.Message);
        }
    }

    private static Result<VortexRecord> Unreadable(string path, string reason) =>
        Result.Failure<VortexRecord>(new Error(DiagnosticCodes.VortexRecordUnreadable, $"Vortex's record {path} could not be read: {reason}.", "Close Vortex and try again."));

    private static string Normal(string path) => path.Replace('\\', '/').TrimEnd('/');

    private static string Text(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;

    // A path that is rooted or climbs out of the record's folder is not one Vortex placed there, so it is skipped.
    private static List<VortexPlacedFile> Placed(JsonElement files, string folder) =>
        files.EnumerateArray()
            .Select(entry => (Path: Normal(Text(entry, "relPath")), Mod: Text(entry, "source")))
            .Where(entry => entry.Path.Length > 0 && entry.Mod.Length > 0 && !entry.Path.StartsWith('/') && !entry.Path.Contains(':', StringComparison.Ordinal)
                && !entry.Path.Split('/').Contains(".."))
            .Select(entry => new VortexPlacedFile(folder + "/" + entry.Path, entry.Mod))
            .ToList();

    private HashSet<string> Staged(string stagingPath)
    {
        var mods = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (stagingPath.Length > 0 && _fileSystem.Directory.Exists(stagingPath))
        {
            mods.UnionWith(_fileSystem.Directory.EnumerateDirectories(stagingPath).Select(d => _fileSystem.Path.GetFileName(d)));
        }

        return mods;
    }
}
