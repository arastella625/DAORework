using Avalonia.Controls.Primitives;

namespace DAORework.App.Controls;

/// <summary>The thin rule with a rune in the middle that sits under a page title (ADR-0016).</summary>
public sealed class RuneDivider : TemplatedControl
{
    protected override Type StyleKeyOverride => typeof(RuneDivider);
}
