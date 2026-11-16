namespace DAORework.App.ViewModels;

/// <summary>
/// What helps when something goes wrong, such as add-ins that share a file, grouped so the Library's actions stay within
/// five dependencies (ENGINEERING_STANDARDS §3).
/// </summary>
public sealed class TroubleshootingViewModel(ProblemSearchViewModel problemSearch, LastGameViewModel lastGame, AddInPriorityViewModel addIns)
{
    public ProblemSearchViewModel ProblemSearch { get; } = problemSearch;

    public LastGameViewModel LastGame { get; } = lastGame;

    /// <summary>Gets each add-in's Priority and the files it shares with other add-ins (P2.5-23).</summary>
    public AddInPriorityViewModel AddIns { get; } = addIns;
}
