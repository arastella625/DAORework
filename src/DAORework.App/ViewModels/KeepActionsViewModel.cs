namespace DAORework.App.ViewModels;

/// <summary><i>Keep this one</i> and <i>Keep one of each</i> (ADR-0039), grouped so the Library's actions stay within five dependencies.</summary>
public sealed class KeepActionsViewModel(KeepCopyViewModel keepCopy, KeepEachViewModel keepEach)
{
    public KeepCopyViewModel KeepCopy { get; } = keepCopy;

    public KeepEachViewModel KeepEach { get; } = keepEach;
}
