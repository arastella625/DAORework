namespace DAORework.Application.Preferences;

/// <summary>Sets the app-wide look to <paramref name="LookId"/>, the id of a theme or of a custom look.</summary>
public sealed record SetLookRequest(string LookId);
