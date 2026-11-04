using DAORework.Core.Common;
using DAORework.Core.Diagnostics;

namespace DAORework.Core.Mods.Library;

/// <summary>Tells from an archive's file list what kind of mod it holds (ROADMAP P2-07). Pure: the caller lists the entries.</summary>
public static class ArchiveClassifier
{
    public static Result<ArchiveClassification> Classify(IReadOnlyList<string> files)
    {
        ArgumentNullException.ThrowIfNull(files);
        if (files.Count == 0)
        {
            return Fail("the archive is empty.");
        }

        var unsafePath = files.FirstOrDefault(f => !ModPath.IsSafe(f));
        if (unsafePath is not null)
        {
            return Fail($"'{unsafePath}' would be written outside the mod's folder.");
        }

        if (IsAddInLayout(files))
        {
            return Result.Success(new ArchiveClassification(ArchiveShape.AddIn));
        }

        var dazips = files.Where(f => f.EndsWith(".dazip", StringComparison.OrdinalIgnoreCase)).ToList();
        return dazips.Count switch
        {
            0 => Result.Success(new ArchiveClassification(GameProgramLayout.Matches(files) ? ArchiveShape.GameProgram : ArchiveShape.Override)),
            1 => Result.Success(new ArchiveClassification(ArchiveShape.WrappedAddIn, dazips[0])),
            _ => Fail($"it holds {dazips.Count} .dazip files ({string.Join(", ", dazips)}); extract them and import each one."),
        };
    }

    /// <summary>Whether the list has a <c>.dazip</c>'s layout: <c>Manifest.xml</c> and <c>Contents/</c> at its root.</summary>
    public static bool IsAddInLayout(IReadOnlyList<string> files)
    {
        ArgumentNullException.ThrowIfNull(files);
        return files.Any(f => f.Equals("Manifest.xml", StringComparison.OrdinalIgnoreCase))
            && files.Any(f => f.StartsWith("Contents/", StringComparison.OrdinalIgnoreCase));
    }

    private static Result<ArchiveClassification> Fail(string reason) =>
        Result.Failure<ArchiveClassification>(new Error(DiagnosticCodes.ModImportInvalid, $"The archive cannot be imported: {reason}"));
}
