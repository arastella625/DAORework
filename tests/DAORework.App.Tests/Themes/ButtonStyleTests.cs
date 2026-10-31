using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.VisualTree;
using DAORework.App.Themes;
using Xunit;

namespace DAORework.App.Tests.Themes;

/// <summary>The primary button takes the accent, and only a destructive confirm is filled red (ADR-0016 rule 3).</summary>
public sealed class ButtonStyleTests
{
    [AvaloniaTheory]
    [InlineData("accent", "Accent", "OnAccent")]
    [InlineData("danger", "Danger", "TextPrimary")]
    public void ClassedButton_IsFilledWithItsTokenAndLabelledToPass(string buttonClass, string fill, string label)
    {
        var button = new Button { Content = buttonClass, Classes = { buttonClass } };
        var window = new Window { Content = button, Width = 200, Height = 80 };
        window.Show();

        var presenter = button.GetVisualDescendants().OfType<ContentPresenter>().First(p => p.Name == "PART_ContentPresenter");
        Assert.Equal(ThemeDictionaries.ColorOf(AppThemes.Default, fill), Assert.IsAssignableFrom<ISolidColorBrush>(presenter.Background).Color);
        Assert.Equal(ThemeDictionaries.ColorOf(AppThemes.Default, label), Assert.IsAssignableFrom<ISolidColorBrush>(presenter.Foreground).Color);
        window.Close();
    }
}
