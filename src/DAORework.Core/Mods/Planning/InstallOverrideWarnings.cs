using DAORework.Core.Mods.Conflicts;

namespace DAORework.Core.Mods.Planning;

/// <summary>
/// What a plan says about the install folder's override (ROADMAP P2.5-22): each file the plan deploys that is there
/// too, the first few by name, and why the folder could not be read if it could not.
/// </summary>
internal static class InstallOverrideWarnings
{
    private const int NamesShown = 10;

    public static IReadOnlyList<string> For(ModActivationPlan plan, Func<string, string> nameOf)
    {
        var warnings = plan.InstallOverrideShadows.Take(NamesShown).Select(file =>
                $"{file.FileName} from {nameOf(file.ModId)} is also in your game folder's override, which DAORework does not manage; the game uses one copy.{Meaning(file.FileName)}")
            .ToList();
        if (plan.InstallOverrideShadows.Count > NamesShown)
        {
            var more = plan.InstallOverrideShadows.Count - NamesShown;
            warnings.Add(more == 1 ? "1 more file is in your game folder's override too." : $"{more} more files are in your game folder's override too.");
        }

        if (plan.InstallOverrideProblem is { } problem)
        {
            warnings.Add(problem);
        }

        return warnings;
    }

    private static string Meaning(string fileName) => SharedFileMeaning.For(fileName) is { } meaning ? " " + meaning : string.Empty;
}
