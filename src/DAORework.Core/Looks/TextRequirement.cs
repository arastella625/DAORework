namespace DAORework.Core.Looks;

/// <summary>A text color and the contrast it needs (<see cref="Contrast.Text"/> or <see cref="Contrast.LargeOrUi"/>).</summary>
public readonly record struct TextRequirement(LookColor Color, double Needed);
