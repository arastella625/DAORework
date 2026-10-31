namespace DAORework.App.ViewModels;

/// <summary>What a suggested next step leads to (ROADMAP P2.5-07). Each one opens a plan; none applies anything by itself.</summary>
public enum NextStepKind
{
    ApplyLargeAddressAware,
    InstallDxvk,
    AdoptHandInstalled,
}

/// <summary>One suggested next step on the Profile tab: why it is suggested, and the button that starts it.</summary>
public sealed record NextStep(NextStepKind Kind, string Title, string Detail, string Action);
