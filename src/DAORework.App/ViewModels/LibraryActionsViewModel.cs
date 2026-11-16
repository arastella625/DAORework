namespace DAORework.App.ViewModels;

/// <summary>What the Library's rows and buttons reach for, grouped so the Mods tab stays within five dependencies (ENGINEERING_STANDARDS §3).</summary>
public sealed class LibraryActionsViewModel(ModPageViewModel modPage, LookFirstViewModel lookFirst, KeepActionsViewModel keep, TroubleshootingViewModel troubleshooting, GameProgramChangeViewModel programs)
{
    public ModPageViewModel ModPage { get; } = modPage;

    public LookFirstViewModel LookFirst { get; } = lookFirst;

    public KeepCopyViewModel KeepCopy { get; } = keep.KeepCopy;

    public KeepEachViewModel KeepEach { get; } = keep.KeepEach;

    public TroubleshootingViewModel Troubleshooting { get; } = troubleshooting;

    /// <summary>Gets turning a mod that runs inside the game on or off, by itself (ADR-0033 §3).</summary>
    public GameProgramChangeViewModel Programs { get; } = programs;
}
