using Avalonia.Controls;
using Avalonia.Threading;
using DAORework.App.ViewModels;

namespace DAORework.App.Views;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Opened += (_, _) => _ = (DataContext as MainWindowViewModel)?.InitializeAsync();

        // A Settings section opened from Home scrolls into view, once its content has been laid out (P2.5-27).
        foreach (var section in new[] { GameFolders, Troubleshooting })
        {
            section.Expanded += (_, _) => Dispatcher.UIThread.Post(() => section.BringIntoView(), DispatcherPriority.Background);
        }
    }
}
