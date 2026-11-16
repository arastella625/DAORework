using DAORework.Core.Mods.Conflicts;

namespace DAORework.Core.Mods.AddIns;

/// <summary>What a player is told about a file add-ins share (P2.5-23): who has it, whose copy the game uses and why. Pure.</summary>
public static class AddInClashWords
{
    /// <summary>What to do about it. A file that needs a compatibility patch says so in its own sentence (<see cref="SharedFileMeaning"/>).</summary>
    public const string Advice = "To use the other copy, turn off the add-in you need less. DAORework doesn't change priorities.";

    public static string Sentence(AddInClash clash)
    {
        ArgumentNullException.ThrowIfNull(clash);
        var names = clash.Others.Select(o => o.Title).Prepend(clash.Winner.Title).ToList();
        var who = names.Count == 2 ? $"both {names[0]} and {names[1]}" : $"{string.Join(", ", names[..^1])} and {names[^1]}";
        var why = clash.Others.All(o => o.Priority == clash.Winner.Priority)
            ? $"they have the same Priority ({clash.Winner.Priority}) and it comes first in AddIns.xml"
            : clash.WonOnPosition
            ? $"its Priority is the lowest ({clash.Winner.Priority}), and it comes first in AddIns.xml"
            : clash.Others.Count == 1
                ? $"its Priority is lower ({clash.Winner.Priority}, against {clash.Others[0].Priority})"
                : $"its Priority is the lowest ({clash.Winner.Priority})";
        var meaning = SharedFileMeaning.For(clash.FileName) is { } said ? " " + said : string.Empty;
        return $"{clash.FileName} is in {who}. The game uses {clash.Winner.Title}'s copy, because {why}.{meaning}";
    }
}
