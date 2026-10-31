using DAORework.Core.Documents;

namespace DAORework.Application.Documents;

/// <summary>Every candidate documents folder, and the one a new profile should use.</summary>
public sealed class DocumentsDetection
{
    public DocumentsDetection(IReadOnlyList<DocumentsFolder> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        if (candidates.Count == 0)
        {
            throw new ArgumentException("At least the known folder candidate is required.", nameof(candidates));
        }

        Candidates = candidates;
    }

    /// <summary>Gets the candidates: existing folders first, then the shell's known folder, then the rest.</summary>
    public IReadOnlyList<DocumentsFolder> Candidates { get; }

    /// <summary>Gets the first existing folder, else the shell's known folder: the game reads and writes there.</summary>
    public DocumentsFolder Recommended => Candidates[0];

    /// <summary>Gets a value indicating whether the recommended folder is not the one the game uses right now.</summary>
    public bool RecommendedDiffersFromKnownFolder => Recommended.Source != DocumentsFolderSource.KnownFolder;
}
