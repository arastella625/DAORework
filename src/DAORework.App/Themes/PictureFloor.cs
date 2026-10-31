using Avalonia.Controls;
using DAORework.Core.Looks;

namespace DAORework.App.Themes;

/// <summary>The lowest veil, in percent, at which a look's text stays readable over its picture (ADR-0017).</summary>
public static class PictureFloor
{
    /// <summary>The floor for <paramref name="background"/> at <paramref name="tint"/>, over the look's colors from <see cref="LookAppearance.Build"/>.</summary>
    public static int Of(LookBackground background, int tint, ResourceDictionary dictionary)
    {
        ArgumentNullException.ThrowIfNull(background);
        var brightest = LookColor.Parse(background.Brightest).TryGetValue(out var measured, out _) ? measured : new LookColor(255, 255, 255); // Unknown: assume white, the safe side.
        return PictureTreatment.DimFloor(
            brightest,
            LookAppearance.ToLook(dictionary, "Background"),
            LookAppearance.ToLook(dictionary, "Accent"),
            tint,
            LookAppearance.TextRequirements(dictionary));
    }
}
