using DAORework.Application.Mods.Apply;
using DAORework.Application.Mods.Library;
using DAORework.Core.Common;
using DAORework.Core.Mods.Planning;

namespace DAORework.App.ViewModels;

/// <summary>How every preview that removes copies ends (ADR-0039): a swap through the usual change, and only then the removal.</summary>
public sealed class CopyRemoval(RemoveCopies remove, RecordSwap recordSwap, ChangeInProgress change)
{
    /// <returns>What went wrong, or null when it all went ahead.</returns>
    public async Task<Error?> RunAsync(string title, ModActivationPlan? swap, string? documents, IReadOnlyList<string> modIds, CancellationToken cancellationToken)
    {
        if (swap is not null && documents is not null)
        {
            var swapped = swap.IsEmpty
                ? (await recordSwap.ExecuteAsync(documents, swap, cancellationToken)).Error
                : (await change.ApplyAsync(title, new ApplyModPlanRequest(documents, swap), cancellationToken)).Error;
            if (swapped is { } failed)
            {
                return failed with { Message = $"Nothing was removed: {failed.Message}" };
            }
        }

        return (await change.RunAsync(title, _ => Task.Run(() => remove.ExecuteAsync(modIds, cancellationToken), cancellationToken))).Error;
    }
}
