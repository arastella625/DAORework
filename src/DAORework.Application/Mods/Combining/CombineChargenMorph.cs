using DAORework.Application.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Mods.Combining;
using DAORework.Core.Mods.Library;

namespace DAORework.Application.Mods.Combining;

/// <summary>
/// Before a plan (ADR-0025): when two or more of the mods to have active ship <c>chargenmorphcfg.xml</c>, their copies
/// are read from the library, combined, and the combined file is kept in the library to be deployed in their place.
/// A copy that cannot be read, or a combined file that cannot be kept, means nothing is combined; the reason becomes a
/// warning in the plan and the copy from the mod last in the load order is used, as without combining.
/// </summary>
public static class CombineChargenMorph
{
    /// <param name="library">The library the mods are read from and the combined file is kept in.</param>
    /// <param name="orderedMods">The mods to have active, lowest priority first.</param>
    /// <param name="cancellationToken">Stops the reads.</param>
    public static async Task<Result<CombineOutcome>> PrepareAsync(IModLibrary library, IReadOnlyList<LibraryMod> orderedMods, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(library);
        if (!CombinedFiles.ChargenMorphCopies(orderedMods).TryGetValue(out var copies, out var error))
        {
            return Result.Failure<CombineOutcome>(error);
        }

        if (copies.Count < 2)
        {
            return Result.Success(CombineOutcome.None);
        }

        var names = orderedMods.ToDictionary(m => m.Id, m => m.Name, StringComparer.OrdinalIgnoreCase);
        var texts = new List<ChargenMorphCopy>();
        foreach (var copy in copies)
        {
            if (!(await library.ReadTextAsync(copy.ModId, copy.Source.RelativePath, cancellationToken)).TryGetValue(out var text, out error))
            {
                return Result.Success(NotCombined(error));
            }

            texts.Add(new ChargenMorphCopy(copy.ModId, text, names.GetValueOrDefault(copy.ModId)));
        }

        if (!ChargenMorphCombiner.Combine(texts).TryGetValue(out var combined, out error)
            || !(await library.StoreCombinedAsync(GamePaths.ChargenMorphFileName, combined.Xml, cancellationToken)).TryGetValue(out var stored, out error))
        {
            return Result.Success(NotCombined(error));
        }

        return Result.Success(new CombineOutcome([new CombinedOverride(CombinedFiles.Deployed(stored), combined.ModIds, combined.EntryCount)], []));
    }

    private static CombineOutcome NotCombined(Error error) =>
        new([], [$"{error.Message} The copies of {GamePaths.ChargenMorphFileName} are not combined; the one from the mod last in the load order is used. ({error.Code})"]);
}
