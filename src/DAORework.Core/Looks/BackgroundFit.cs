namespace DAORework.Core.Looks;

/// <summary>How a background picture covers the window.</summary>
public enum BackgroundFit
{
    /// <summary>Covers the whole window, cropping what does not fit.</summary>
    Fill,

    /// <summary>Shows the whole picture, with the theme's background around it.</summary>
    Fit,
}
