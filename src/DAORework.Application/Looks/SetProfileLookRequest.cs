namespace DAORework.Application.Looks;

/// <summary>Gives the launch profile <paramref name="ProfileName"/> its own look, or none (null) to use the app-wide one.</summary>
public sealed record SetProfileLookRequest(string ProfileName, string? LookId);
