using Avalonia.Controls;
using DAORework.App.ViewModels;

namespace DAORework.App.Views;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Opened += (_, _) => _ = (DataContext as MainWindowViewModel)?.InitializeAsync();
    }
}
