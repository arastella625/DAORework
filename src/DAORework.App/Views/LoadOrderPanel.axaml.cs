using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using DAORework.App.ViewModels;

namespace DAORework.App.Views;

/// <summary>
/// The Mods tab's <i>Load order</i>: the active mods in order and the files they share (P2.5-05). A mod is moved by its
/// grip (ADR-0038) with plain pointer events, since it never leaves this list: dropping it plans one move, like the buttons.
/// </summary>
public sealed partial class LoadOrderPanel : UserControl
{
    private LoadOrderRow? _dragged;

    public LoadOrderPanel()
    {
        InitializeComponent();
        Order.AddHandler(PointerPressedEvent, OnPressed, RoutingStrategies.Bubble);
        Order.AddHandler(PointerMovedEvent, OnMoved, RoutingStrategies.Bubble);
        Order.AddHandler(PointerReleasedEvent, OnReleased, RoutingStrategies.Bubble);
        Order.AddHandler(PointerCaptureLostEvent, (_, _) => EndDrag(), RoutingStrategies.Bubble);
    }

    private void OnPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source is StyledElement { DataContext: LoadOrderRow row } grip && grip.Classes.Contains("dragGrip")
            && e.GetCurrentPoint(Order).Properties.IsLeftButtonPressed)
        {
            _dragged = row;
            e.Pointer.Capture(Order);
            e.Handled = true;
        }
    }

    private void OnMoved(object? sender, PointerEventArgs e)
    {
        if (_dragged is null)
        {
            return;
        }

        var rows = Rows();
        var gap = ArrangeDrop.Gap(Middles(rows), e.GetPosition(Order).Y);
        DropLine.Margin = new Thickness(0, gap == 0 ? 0 : rows[gap - 1].Bounds.Bottom, 0, 0);
        DropLine.IsVisible = true;
    }

    private void OnReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_dragged is { } row && DataContext is LoadOrderViewModel order)
        {
            var gap = ArrangeDrop.Gap(Middles(Rows()), e.GetPosition(Order).Y);
            e.Pointer.Capture(null);
            _ = order.MoveToPlaceAsync(row, ArrangeDrop.Place(row.Position - 1, gap));
        }

        EndDrag();
    }

    private void EndDrag()
    {
        _dragged = null;
        DropLine.IsVisible = false;
    }

    private List<Control> Rows() =>
        [.. Enumerable.Range(0, Order.ItemCount).Select(Order.ContainerFromIndex).OfType<Control>()];

    private static List<double> Middles(List<Control> rows) => [.. rows.Select(r => r.Bounds.Top + (r.Bounds.Height / 2))];
}
