using Avalonia.Controls;
using Avalonia.Interactivity;

namespace DAORework.App.Views;

/// <summary>Says the app is already open, and closes this copy (ADR-0044).</summary>
public sealed partial class AlreadyOpenWindow : Window
{
    public AlreadyOpenWindow() => InitializeComponent();

    private void OnClose(object? sender, RoutedEventArgs e) => Close();
}
