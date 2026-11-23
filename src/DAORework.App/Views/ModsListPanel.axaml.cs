using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DAORework.App.ViewModels;

namespace DAORework.App.Views;

public sealed partial class ModsListPanel : UserControl
{
    private Vector _listOffset;
    private ModsTabViewModel? _tab;

    public ModsListPanel()
    {
        InitializeComponent();
    }

    /// <summary>The page keeps its place between tabs, so what Home opens, such as <i>Find the problem mod</i>, could be out of sight (P2.6-63).</summary>
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (_tab is not null)
        {
            _tab.Filter.OpenedFromHome -= BringCardsIntoView;
            _tab.About.OpenedOrClosed -= KeepTheListsPlace;
        }

        _tab = DataContext as ModsTabViewModel;
        if (_tab is not null)
        {
            _tab.Filter.OpenedFromHome += BringCardsIntoView;
            _tab.About.OpenedOrClosed += KeepTheListsPlace;
        }
    }

    private void BringCardsIntoView(object? sender, EventArgs e) =>
        Dispatcher.UIThread.Post(() => KindCards.BringIntoView(), DispatcherPriority.Background);

    // The page starts at its top with focus on Back, and Back to your mods returns to the same mod's row, focused
    // (ADR-0050 §3). The row is brought into view by itself, since rows may have come or gone while the page was open.
    private void KeepTheListsPlace(object? sender, EventArgs e)
    {
        if (_tab is null || this.FindAncestorOfType<ScrollViewer>() is not { } scroller)
        {
            return;
        }

        if (_tab.About.IsOpen)
        {
            _listOffset = scroller.Offset;
            scroller.Offset = default;
            Dispatcher.UIThread.Post(() => AboutMod.FindControl<Button>("BackToModsButton")?.Focus(), DispatcherPriority.Background);
            return;
        }

        var offset = _listOffset;
        var row = _tab.About.LastShown is { } shown ? _tab.Library.Mods.FirstOrDefault(m => m.Id == shown.Id) : null;
        Dispatcher.UIThread.Post(
            () =>
            {
                scroller.Offset = offset;
                if (row is not null && this.GetLogicalDescendants().OfType<ItemsControl>().FirstOrDefault(i => i.Name == "LibraryMods") is { } list)
                {
                    list.ScrollIntoView(row);
                    list.ContainerFromItem(row)?.GetLogicalDescendants().OfType<Button>().FirstOrDefault(b => b.Name == "ModDetails")?.Focus();
                }
            },
            DispatcherPriority.Background);
    }
}
