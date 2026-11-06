using Avalonia.Controls;

namespace DAORework.App.Views;

/// <summary>Every preview on the Mods tab, docked below the list so its buttons always show (ADR-0038).</summary>
public sealed partial class PreviewSheet : UserControl
{
    public PreviewSheet()
    {
        InitializeComponent();
    }
}
