using System.ComponentModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DAORework.App.ViewModels;

/// <summary>
/// What the window is working on, for its footer (P2.6-45): without this a wait looks the same as nothing happening. The
/// panels watched get a moment to paint before they start, since work on the window's thread would hold the line back.
/// A change that is running has its own banner, and a panel stays busy through it, so the line says nothing then.
/// </summary>
public sealed partial class WorkingLine : ObservableObject
{
    private readonly List<(PageViewModel Page, string What)> _watched = [];
    private readonly ChangeInProgress _change;

    public WorkingLine(ChangeInProgress change)
    {
        ArgumentNullException.ThrowIfNull(change);
        _change = change;
        change.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ChangeInProgress.IsRunning))
            {
                Update();
            }
        };
    }

    /// <summary>Gets what is being done, or null when nothing is.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsWorking))]
    public partial string? Text { get; private set; }

    public bool IsWorking => Text is not null;

    /// <param name="page">The panel.</param>
    /// <param name="what">What it says while it is busy.</param>
    public void Watch(PageViewModel page, string what)
    {
        ArgumentNullException.ThrowIfNull(page);
        _watched.Add((page, what));
        page.PropertyChanged += OnPageChanged;
        page.BeforeWorkAsync = PaintFirstAsync;
    }

    private static Task PaintFirstAsync() => Dispatcher.UIThread.InvokeAsync(static () => { }, DispatcherPriority.Background).GetTask();

    private void OnPageChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PageViewModel.IsBusy))
        {
            Update();
        }
    }

    private void Update() => Text = _change.IsRunning ? null : _watched.Where(w => w.Page.IsBusy).Select(w => w.What).LastOrDefault();
}
