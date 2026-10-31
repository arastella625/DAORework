namespace DAORework.Core.Looks;

/// <summary>
/// What an accent has to stand out against, from the look's base theme: the label on the accent button, and the
/// surfaces accent text and the focus ring sit on (the page, panels, cards, and the page where its smoke is strongest).
/// </summary>
public sealed record AccentSurfaces(LookColor OnAccent, IReadOnlyList<LookColor> Surfaces);
