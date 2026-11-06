namespace DAORework.Core.Mods;

/// <summary>The one thing to do next, and its button; a null <see cref="Button"/> needs none.</summary>
public sealed record NextMove(NextMoveKind Kind, string Title, string Body, string? Button);
