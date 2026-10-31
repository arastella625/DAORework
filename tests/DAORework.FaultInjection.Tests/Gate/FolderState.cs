using System.Security.Cryptography;
using System.Text.Json;
using DAORework.Core.Mods.Backups;

namespace DAORework.FaultInjection.Tests.Gate;

/// <summary>
/// What a run must leave exactly as the before- or the after-state (ADR-0014): every file under the documents and
/// install folders with its SHA-256, including any temporary file left behind, and each folder's active list.
/// </summary>
internal static class FolderState
{
    public static IReadOnlyList<string> Take(GateFolders folders)
    {
        var lines = new List<string>();
        foreach (var (label, folder) in new[] { ("documents", folders.Documents), ("install", folders.Install) })
        {
            lines.AddRange(Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories)
                .Select(f => $"{label}/{Path.GetRelativePath(folder, f).Replace('\\', '/')}={Sha(f)}")
                .Order(StringComparer.Ordinal));
            lines.Add($"{label} active: {string.Join(", ", ActiveIds(folders, folder))}");
        }

        return lines;
    }

    public static bool HasJournal(GateFolders folders, string folder) =>
        File.Exists(Path.Combine(folders.AppData, "deployments", DocumentsKey.For(folder) + ".journal.json"));

    public static string Diff(IReadOnlyList<string> expected, IReadOnlyList<string> actual)
    {
        var missing = expected.Except(actual).Take(3).Select(l => $"missing {l}");
        var extra = actual.Except(expected).Take(3).Select(l => $"unexpected {l}");
        return string.Join("; ", missing.Concat(extra));
    }

    private static string Sha(string path) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)))[..16];

    private static List<string> ActiveIds(GateFolders folders, string folder)
    {
        var record = Path.Combine(folders.AppData, "deployments", DocumentsKey.For(folder) + ".json");
        if (!File.Exists(record))
        {
            return [];
        }

        using var json = JsonDocument.Parse(File.ReadAllText(record));
        var active = json.RootElement.EnumerateObject().First(p => p.Name.Equals("activeModIds", StringComparison.OrdinalIgnoreCase)).Value;
        return active.EnumerateArray().Select(e => e.GetString() ?? string.Empty).ToList();
    }
}
