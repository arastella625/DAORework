namespace DAORework.App.ViewModels;

/// <summary>What the Library's rows and buttons reach for, grouped so the Mods tab stays within five dependencies (ENGINEERING_STANDARDS §3).</summary>
public sealed class LibraryActionsViewModel(ModPageViewModel modPage, LookFirstViewModel lookFirst, KeepCopyViewModel keepCopy, KeepEachViewModel keepEach)
{
    public ModPageViewModel ModPage { get; } = modPage;

    public LookFirstViewModel LookFirst { get; } = lookFirst;

    public KeepCopyViewModel KeepCopy { get; } = keepCopy;

    public KeepEachViewModel KeepEach { get; } = keepEach;
}
