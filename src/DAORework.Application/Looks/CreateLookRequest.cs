namespace DAORework.Application.Looks;

/// <summary>A new look named <paramref name="Name"/>, starting as a copy of the built-in theme <paramref name="BaseTheme"/>.</summary>
public sealed record CreateLookRequest(string Name, string BaseTheme);
