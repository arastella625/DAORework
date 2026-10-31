using Avalonia.Media;

namespace DAORework.App.Themes;

/// <summary>
/// A look's picture as the window draws it (ADR-0017): already tinted, how it fills the window, its blur, and the veil
/// of the theme's background over it, 0 to 1 and never below the floor.
/// </summary>
public sealed record LookPictureLayer(IImage Picture, Stretch Stretch, double Blur, double Veil)
{
    public const string PictureKey = "LookPicture";
    public const string StretchKey = "LookPictureStretch";
    public const string BlurKey = "LookPictureBlur";
    public const string VeilKey = "LookVeilOpacity";
}
