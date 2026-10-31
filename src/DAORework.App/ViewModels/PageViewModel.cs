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

    protected void ReportSuccess(string message)
    {
        IsError = false;
        StatusMessage = message;
    }

    protected void ReportError(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        IsError = true;
        StatusMessage = string.IsNullOrWhiteSpace(error.Recommendation)
            ? $"{error.Message} ({error.Code})"
            : $"{error.Message} {error.Recommendation} ({error.Code})";
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
