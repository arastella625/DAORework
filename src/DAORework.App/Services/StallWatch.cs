using System.Diagnostics;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;

namespace DAORework.App.Services;

/// <summary>
/// Notes in the log when the window's thread was held (P2.6-44). A tick that arrives late means nothing could repaint or
/// highlight meanwhile, so a real run shows how long the window really stalled, on a library no test has.
/// </summary>
public sealed class StallWatch
{
    /// <summary>The longest the thread should be held (ROADMAP, <i>Feeling quick</i>).</summary>
    public static readonly TimeSpan Limit = TimeSpan.FromMilliseconds(200);

    private static readonly TimeSpan _tick = TimeSpan.FromMilliseconds(200);

    private readonly ILogger<StallWatch> _logger;
    private long _last;

    public StallWatch(ILogger<StallWatch> logger)
    {
        _logger = logger;
    }

    /// <summary>Whether a gap between two ticks means the thread was held too long.</summary>
    public static bool IsStall(TimeSpan gap) => gap - _tick > Limit;

    public void Start()
    {
        _last = Stopwatch.GetTimestamp();
        var timer = new DispatcherTimer(_tick, DispatcherPriority.Input, (_, _) => OnTick());
        timer.Start();
    }

    private void OnTick()
    {
        var gap = Stopwatch.GetElapsedTime(_last);
        _last = Stopwatch.GetTimestamp();
        if (IsStall(gap))
        {
            _logger.LogInformation("The window was held for {Milliseconds} ms.", (long)gap.TotalMilliseconds);
        }
    }
}
