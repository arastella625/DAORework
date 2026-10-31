namespace DAORework.Core.Looks;

/// <summary>
/// The accent tokens derived from one pick (ADR-0017): the button fill, accent text and the focus glow, all in the
/// pick's hue. <see cref="Darkened"/> and <see cref="Lightened"/> say whether the editor should tell the user why the
/// result looks different from the pick.
/// </summary>
public sealed record AccentPalette(LookColor Accent, LookColor AccentText, LookColor FocusGlow, bool Darkened, bool Lightened);
