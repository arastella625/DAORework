namespace DAORework.App.ViewModels;

/// <summary>One thing a set can keep: a copy, or with no <paramref name="ModId"/> all of them.</summary>
public sealed record KeepChoice(string Label, string? ModId);
