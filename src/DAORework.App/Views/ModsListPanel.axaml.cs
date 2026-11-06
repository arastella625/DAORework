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

    /// <summary>The page keeps its place between tabs, so <i>Find the problem mod</i> could otherwise open out of sight (P2.6-63).</summary>
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is ModsTabViewModel { Filter: var filter })
        {
            filter.PropertyChanged += (_, changed) =>
            {
                if (changed.PropertyName == nameof(ModListFilter.ShowsCombined) && filter.ShowsCombined)
                {
                    Dispatcher.UIThread.Post(() => FindProblemPanel.BringIntoView(), DispatcherPriority.Background);
                }
            };
        }
    }
}
