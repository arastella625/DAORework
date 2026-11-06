namespace DAORework.App.ViewModels;

/// <summary>Where a mod dragged in <i>Arrange</i> lands (ADR-0038). Pure, so the arithmetic is tested without a pointer.</summary>
public static class ArrangeDrop
{
    /// <summary>The gap the pointer is in: how many rows have their middle above it.</summary>
    public static int Gap(IReadOnlyList<double> rowMiddles, double y)
    {
        ArgumentNullException.ThrowIfNull(rowMiddles);
        return rowMiddles.Count(m => m < y);
    }

    /// <summary>The mod's new place. Taking it out first moves every later gap up by one.</summary>
    public static int Place(int from, int gap) => gap > from ? gap - 1 : gap;
}
