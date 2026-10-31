using DAORework.Application.Fixes;

namespace DAORework.App.ViewModels;

/// <summary>
/// A fix DAORework applied, on the Fixes tab (P2.5-06): in effect, or undone outside DAORework, which is what Steam's
/// <i>Verify integrity</i> or a game update does when it puts the original files back (ADR-0013).
/// </summary>
public sealed record AppliedFixRow(string Title, bool InEffect, string State)
{
    public static AppliedFixRow From(AppliedFix fix)
    {
        ArgumentNullException.ThrowIfNull(fix);
        var title = fix.Version is null ? fix.FixName : $"{fix.FixName} {fix.Version}";
        return new AppliedFixRow(
            title,
            fix.InEffect,
            fix.InEffect
                ? "In effect."
                : $"No longer in effect: {string.Join(", ", fix.NotInEffect)} changed outside DAORework since it was applied. Steam's Verify integrity or a game update does this when it puts the original files back; apply the fix again to restore it.");
    }
}
