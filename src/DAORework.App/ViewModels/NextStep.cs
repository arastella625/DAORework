namespace DAORework.App.ViewModels;

/// <summary>What a suggested next step leads to (ROADMAP P2.5-07). Each one opens a plan; none applies anything by itself.</summary>
public enum NextStepKind
{
    ApplyLargeAddressAware,
    InstallDxvk,
    MoveFromVortex,

    /// <summary>The profile's documents folder isn't the game's (P2.6-75, ADR-0046); opens <i>Game folders</i>.</summary>
    ChooseGameDocumentsFolder,
}

/// <summary>One suggested next step on the Profile tab: why it is suggested, and the button that starts it.</summary>
public sealed record NextStep(NextStepKind Kind, string Title, string Detail, string Action);
