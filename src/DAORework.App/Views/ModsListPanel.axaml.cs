using Avalonia.Controls;
using Avalonia.Threading;
using DAORework.App.ViewModels;

namespace DAORework.App.Views;

public sealed partial class ModsListPanel : UserControl
{
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
        }
    }
}
