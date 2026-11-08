using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Styling;

namespace DAORework.App.Views;

/// <summary>
/// Pages scroll to a control that gets focus only when the player moved there with the keyboard (P2.6-68). Coming back to
/// the window, it gives focus back to the control that had it, and the page scrolled to that control. On the owner's PC
/// that moved the page under the click that brought the window back, so <i>Start</i> was never pressed.
/// </summary>
internal static class FocusScrolling
{
    public static void ByKeyboardOnly(Window window)
    {
        window.Styles.Add(new Style(x => x.OfType<ScrollViewer>()) { Setters = { new Setter(ScrollViewer.BringIntoViewOnFocusChangeProperty, false) } });
        window.AddHandler(InputElement.GotFocusEvent, (_, e) =>
        {
            if (e.NavigationMethod is NavigationMethod.Tab or NavigationMethod.Directional && e.Source is Control control)
            {
                control.BringIntoView();
            }
        });
    }
}
