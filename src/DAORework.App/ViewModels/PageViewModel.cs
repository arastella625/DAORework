using CommunityToolkit.Mvvm.ComponentModel;
using DAORework.Core.Common;
using Microsoft.Extensions.Logging;

namespace DAORework.App.ViewModels;

/// <summary>
/// What every page shares: a status line and one guarded way to run a use case. Expected failures arrive as
/// <see cref="Error"/>s and are shown; an unexpected exception is logged and shown, never lost and never a crash.
/// </summary>
public abstract partial class PageViewModel : ObservableObject
{
    private readonly ILogger _logger;
    private int _shown;

    protected PageViewModel(ILogger logger)
    {
        _logger = logger;
        StatusMessage = string.Empty;
    }

    [ObservableProperty]
    public partial string StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsError { get; set; }

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    /// <summary>Gets or sets how long a success message stays before it fades (docs/VOICE.md rule 4).</summary>
    public TimeSpan SuccessFadesAfter { get; set; } = TimeSpan.FromSeconds(6);

    /// <summary>Says what just happened. The message fades after <see cref="SuccessFadesAfter"/>, unless another replaces it first.</summary>
    protected void ReportSuccess(string message)
    {
        Show(message, isError: false);
        if (message.Length > 0)
        {
            _ = FadeAsync(_shown);
        }
    }

    /// <summary>Says how things stand (a count, a verdict, a question awaiting a second click). It stays until replaced.</summary>
    protected void ReportStatus(string message) => Show(message, isError: false);

    /// <summary>Says what went wrong. An error stays until it is dealt with.</summary>
    protected void ReportError(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        Show(
            string.IsNullOrWhiteSpace(error.Recommendation)
                ? $"{error.Message} ({error.Code})"
                : $"{error.Message} {error.Recommendation} ({error.Code})",
            isError: true);
    }

    private void Show(string message, bool isError)
    {
        _shown++;
        IsError = isError;
        StatusMessage = message;
    }

    // Clears the message only if it is still the one this fade was started for.
    private async Task FadeAsync(int shown)
    {
        await Task.Delay(SuccessFadesAfter);
        if (shown == _shown)
        {
            StatusMessage = string.Empty;
        }
    }

    protected async Task GuardAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(action);
        IsBusy = true;
        try
        {
            await action(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            ReportSuccess("Cancelled.");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error in {ViewModel}", GetType().Name);
            IsError = true;
            StatusMessage = $"Unexpected error: {exception.Message} (see the log in the app-data folder)";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
