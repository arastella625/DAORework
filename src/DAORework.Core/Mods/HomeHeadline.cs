namespace DAORework.Core.Mods;

/// <summary>Home's one card: a headline, one sentence and one button (ADR-0050 §1); a null <see cref="Button"/> needs none.</summary>
public sealed record HomeHeadline(HomeHeadlineKind Kind, string Title, string Sentence, string? Button, HomeButton Action);
