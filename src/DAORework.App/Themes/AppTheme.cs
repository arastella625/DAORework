using Avalonia.Styling;

namespace DAORework.App.Themes;

/// <summary>One of the built-in themes (ADR-0016). <see cref="Id"/> is what a saved preference names.</summary>
public sealed record AppTheme(string Id, string Name, string Description, ThemeVariant Variant, Uri Source);
