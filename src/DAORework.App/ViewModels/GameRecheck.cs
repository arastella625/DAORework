namespace DAORework.App.ViewModels;

/// <summary>
/// Checks the game when the window opens and after each change (ADR-0050 §1), in the background and never two at once:
/// asked while a check runs, it runs one more when that ends, so the last change is always checked. Only the UI thread
/// calls it, so it needs no lock.
/// </summary>
public sealed class GameRecheck
{
    private readonly Func<Task> _check;
    private bool _again;

    public GameRecheck(Func<Task> check)
    {
        ArgumentNullException.ThrowIfNull(check);
        _check = check;
    }

    /// <summary>Gets the checks running now, done when the last one asked for has ended.</summary>
    public Task Running { get; private set; } = Task.CompletedTask;

    /// <summary>Starts a check, or one more after the one running; nobody waits on it.</summary>
    public void Request()
    {
        if (!Running.IsCompleted)
        {
            _again = true;
            return;
        }

        Running = RunAsync();
    }

    private async Task RunAsync()
    {
        do
        {
            _again = false;
            await _check();
        }
        while (_again);
    }
}
