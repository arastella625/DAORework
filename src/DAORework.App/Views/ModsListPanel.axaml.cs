using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DAORework.App.ViewModels;

namespace DAORework.App.Views;

public sealed partial class ModsListPanel : UserControl
{
    private Vector _listOffset;

    public ModsListPanel()
    {
        InitializeComponent();
    }

    /// <summary>The page keeps its place between tabs, so what Home opens, such as <i>Find the problem mod</i>, could be out of sight (P2.6-63).</summary>
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is ModsTabViewModel tab)
        {
            tab.Filter.OpenedFromHome += (_, _) => Dispatcher.UIThread.Post(() => KindCards.BringIntoView(), DispatcherPriority.Background);
            tab.About.OpenedOrClosed += (_, _) => KeepTheListsPlace(tab.About.IsOpen);
        }
    }

    // The page starts at its top, and Back to your mods returns to the same place in the list (ADR-0050 §3).
    private void KeepTheListsPlace(bool opened)
    {
        if (this.FindAncestorOfType<ScrollViewer>() is not { } scroller)
        {
            return;
        }

        if (opened)
        {
            _listOffset = scroller.Offset;
            scroller.Offset = default;
            return;
        }

        var offset = _listOffset;
        Dispatcher.UIThread.Post(() => scroller.Offset = offset, DispatcherPriority.Background); // Once the list is laid out again.
    }
}
