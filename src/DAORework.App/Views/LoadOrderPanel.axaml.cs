using Avalonia.Controls;
using Avalonia.Threading;
using DAORework.App.ViewModels;

namespace DAORework.App.Views;

/// <summary>The Mods tab's <i>Load order</i>: the active mods in order, moved up or down, and the files they share (P2.5-05).</summary>
public sealed partial class LoadOrderPanel : UserControl
{
    public LoadOrderPanel()
    {
        InitializeComponent();
    }

    /// <summary>The preview is below the list, so <i>To top</i> or <i>To bottom</i> pressed far from it would otherwise seem to do nothing.</summary>
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is LoadOrderViewModel order)
        {
            order.MovedFar += (_, _) => Dispatcher.UIThread.Post(() => OrderPlan.BringIntoView(), DispatcherPriority.Loaded);
        }
    }
}
