using DAORework.Application.Mods.Apply;
using DAORework.Application.Mods.Library;
using DAORework.Core.Common;
using DAORework.Core.Mods.Planning;

namespace DAORework.App.ViewModels;

/// <summary>How every preview that removes copies ends (ADR-0039): a swap through the usual change, and only then the removal.</summary>
public sealed class CopyRemoval(RemoveCopies remove, RecordSwap recordSwap, ChangeInProgress change)
{
    /// <summary>Gets a value indicating whether what is removed can still be put back, which decides what the previews say.</summary>
    public bool GoesToRecycleBin => remove.GoesToRecycleBin;

    /// <returns>What went wrong, or null when it all went ahead.</returns>
    public async Task<Error?> RunAsync(string title, ModActivationPlan? swap, string? documents, IReadOnlyList<string> modIds, CancellationToken cancellationToken)
    {
        if (swap is not null && documents is not null)
        {
            if (await SwapAsync(title, swap, documents, cancellationToken) is { } failed)
            {
                return failed with { Message = $"Nothing was removed: {failed.Message}" };
            }
        }

        return (await change.RunAsync(title, _ => Task.Run(() => remove.ExecuteAsync(modIds, cancellationToken), cancellationToken))).Error;
    }

    private async Task<Error?> SwapAsync(string title, ModActivationPlan swap, string documents, CancellationToken cancellationToken)
    {
        if ((await recordSwap.CheckAsync(documents, swap, cancellationToken)).Error is { } stale)
        {
            return stale;
        }

        if (!swap.IsEmpty)
        {
            return (await change.ApplyAsync(title, new ApplyModPlanRequest(documents, swap), cancellationToken)).Error;
        }

        await recordSwap.ExecuteAsync(documents, swap, cancellationToken);
        return null;
    }
}
