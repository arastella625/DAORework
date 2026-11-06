namespace DAORework.App.ViewModels;

/// <summary>What helps when something goes wrong, grouped so the Library's actions stay within five dependencies (ENGINEERING_STANDARDS §3).</summary>
public sealed class TroubleshootingViewModel(ProblemSearchViewModel problemSearch, LastGameViewModel lastGame)
{
    public ProblemSearchViewModel ProblemSearch { get; } = problemSearch;

    public LastGameViewModel LastGame { get; } = lastGame;
}
