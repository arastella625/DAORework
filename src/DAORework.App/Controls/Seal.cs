using Avalonia.Controls;

namespace DAORework.App.Controls;

/// <summary>
/// A wax seal around a small icon (ADR-0026 §4): green when a part is ready, amber with the class <c>warning</c> when it
/// needs a look. Its look is in Themes/Controls.axaml.
/// </summary>
public sealed class Seal : ContentControl
{
    protected override Type StyleKeyOverride => typeof(Seal);
}
