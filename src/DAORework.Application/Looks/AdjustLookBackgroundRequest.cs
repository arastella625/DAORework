using DAORework.Core.Looks;

namespace DAORework.Application.Looks;

/// <summary>
/// How a look's picture is blended in. <paramref name="DimFloor"/> is the lowest dim at which text stays readable, which
/// the App computes with <see cref="PictureTreatment.DimFloor"/> from the look's base theme, accent and tint.
/// </summary>
public sealed record AdjustLookBackgroundRequest(string LookId, BackgroundFit Fit, int Tint, int Dim, int Blur, int DimFloor);
