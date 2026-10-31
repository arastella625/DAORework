namespace DAORework.Core.Looks;

/// <summary>Hue in degrees (0 to 360), saturation and lightness (0 to 1).</summary>
public readonly record struct Hsl(double Hue, double Saturation, double Lightness);
