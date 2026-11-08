using CommunityToolkit.Mvvm.Input;
using DAORework.Core.Mods.Troubleshooting;

namespace DAORework.App.ViewModels;

/// <summary>Keeps the search in step with what is on (P2.6-69): a change made outside it, such as <i>Undo</i>, stops the answers.</summary>
public sealed partial class ProblemSearchViewModel
{
    /// <summary>What a round says when its mods aren't the ones on.</summary>
    public const string OutOfStepNote = "Your mods changed since this round began, so an answer now wouldn't tell us anything. Put this round's mods back to carry on.";

    /// <summary>Gets a value indicating whether the mods on now aren't the ones this round tests.</summary>
    public bool IsOutOfStep => Search is { End: ProblemSearchEnd.None } search && ActiveMods is { } active && !ProblemSearchStep.InStep(search, active());

    /// <summary>Gets a value indicating whether the player can say what happened this round.</summary>
    public bool CanAnswer => IsTesting && !IsOutOfStep;

    /// <summary>Looks again at what is on, after any change.</summary>
    public void Recheck()
    {
        OnPropertyChanged(nameof(IsOutOfStep));
        OnPropertyChanged(nameof(CanAnswer));
    }

    [RelayCommand]
    private Task PutRoundBackAsync(CancellationToken cancellationToken) => GuardAsync(
        token => Search is { End: ProblemSearchEnd.None } search ? PlanRoundAsync(search, token) : Task.CompletedTask,
        cancellationToken);
}
