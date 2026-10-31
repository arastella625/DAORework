using Avalonia.Controls;

namespace DAORework.App.Controls;

/// <summary>A panel with a thin gold frame and corner filigree (ADR-0016). Its look is in Themes/Controls.axaml.</summary>
public sealed class Card : ContentControl
{
    protected override Type StyleKeyOverride => typeof(Card);
}
