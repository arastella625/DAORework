using Avalonia.Data.Converters;

namespace DAORework.App.Views;

/// <summary>
/// How tall a preview sheet's parts can be when it holds a long list, so the sheet and its buttons fit a window of any
/// height and the mods list above keeps a share of what's left (ADR-0038). Checked at the smallest window by RealScaleKeepEachTests.
/// </summary>
public static class SheetHeights
{
    private const double SmallestPlan = 110;
    private const double TallestPlan = 180;
    private const double PlanShare = 0.24;

    /// <summary>What the title, the tabs, the footer and the sheet's buttons take up.</summary>
    private const double TakenUp = 300;
    private const double SmallestList = 100;
    private const double ListShare = 0.6;

    /// <summary>Gets the height for the plan above the list, enough for its summary, notes and <i>Show details</i>.</summary>
    public static FuncValueConverter<double, double> PlanFor { get; } = new(window => Math.Clamp(window * PlanShare, SmallestPlan, TallestPlan));

    /// <summary>Gets the height for the list.</summary>
    public static FuncValueConverter<double, double> ListFor { get; } = new(window => Math.Max(SmallestList, (window - TakenUp) * ListShare));
}
