namespace DAORework.App.ViewModels;

/// <summary>A look a profile can show: a built-in theme, a custom look, or none of its own (a null <paramref name="Id"/>).</summary>
public sealed record LookChoice(string? Id, string Name, string Detail);
