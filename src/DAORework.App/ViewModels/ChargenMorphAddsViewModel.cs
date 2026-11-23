using DAORework.Application.Mods.Combining;
using DAORework.Application.Profiles;
using DAORework.Core.Mods.Combining;
using DAORework.Core.Mods.Library;

namespace DAORework.App.ViewModels;

/// <summary>
/// What each mod adds to character creation, and what a change gained (P2.10-03, ADR-0050 §4), counted against the
/// game's own copy in the profile's install folder where it can be read. The Library asks for it as it reads its mods;
/// a turned-on message and <i>Where mods overlap</i> use the last count.
/// </summary>
public sealed class ChargenMorphAddsViewModel(GetProfile getProfile, CountChargenMorphAdds count)
{
    private string? _profileName;

    /// <summary>Gets the last count, for the mods the Library last read.</summary>
    public ChargenMorphTally Tally { get; private set; } = ChargenMorphTally.Empty;

    public void Load(string? profileName)
    {
        _profileName = profileName;
        Tally = ChargenMorphTally.Empty;
    }

    /// <summary>Counts the Library's mods; without an install folder, against what their copies share.</summary>
    public async Task<ChargenMorphTally> CountAsync(IReadOnlyList<LibraryMod> mods, CancellationToken cancellationToken)
    {
        var profileName = _profileName;
        var install = (await ProfileInstall.ForAsync(getProfile, profileName, cancellationToken)).TryGetValue(out var folder, out _) ? folder : null;
        var tally = await count.ExecuteAsync(mods, install, cancellationToken);
        if (profileName == _profileName)
        {
            Tally = tally; // A count for a profile left meanwhile is dropped.
        }

        return tally;
    }

    /// <summary><i>Character creation has 48 more hairstyles</i>, going from the mods in <paramref name="before"/> to those in <paramref name="after"/>; null when it can't be said in counts.</summary>
    public string? Gained(IEnumerable<string> before, IEnumerable<string> after) => Tally.Gained(before, after)?.Gained();
}
