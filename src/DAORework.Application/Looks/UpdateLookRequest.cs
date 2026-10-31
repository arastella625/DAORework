namespace DAORework.Application.Looks;

/// <summary>A look's new name, base theme and accent (<c>#RRGGBB</c>, or null for the base theme's own).</summary>
public sealed record UpdateLookRequest(string LookId, string Name, string BaseTheme, string? Accent);
