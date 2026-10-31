namespace DAORework.Application.Looks;

/// <summary>Gives the look <paramref name="LookId"/> a copy of the picture at <paramref name="SourcePath"/> as its background.</summary>
public sealed record SetLookBackgroundRequest(string LookId, string SourcePath);
