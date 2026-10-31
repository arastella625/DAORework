using Avalonia.Controls;

namespace DAORework.App.Controls;

/// <summary>
/// The page behind the content: the theme's background, a faint glow of its emphasis color and a vellum grain
/// (ADR-0016). The grain is drawn under the content at a few percent, so it barely moves any contrast.
/// </summary>
public sealed class Backdrop : ContentControl
{
    protected override Type StyleKeyOverride => typeof(Backdrop);
}
