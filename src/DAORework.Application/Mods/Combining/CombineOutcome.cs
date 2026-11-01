using DAORework.Core.Mods.Combining;

namespace DAORework.Application.Mods.Combining;

/// <summary>What combining found before a plan (ADR-0025): the combined files to deploy, and why any copies were not combined.</summary>
public sealed record CombineOutcome(IReadOnlyList<CombinedOverride> Combined, IReadOnlyList<string> Problems)
{
    public static CombineOutcome None { get; } = new([], []);

    /// <summary>Gets the documents paths the combined files go to, which the scan before a plan must hash.</summary>
    public IEnumerable<string> TargetPaths => Combined.Select(c => c.File.TargetPath);
}
