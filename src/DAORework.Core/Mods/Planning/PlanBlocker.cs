namespace DAORework.Core.Mods.Planning;

/// <summary>A reason the plan cannot be applied as it is, with the path or UID it concerns and what to tell the user.</summary>
public sealed record PlanBlocker(PlanBlockerKind Kind, string Subject, string Message)
{
    /// <summary>Gets the mods it concerns, in load order; empty when it isn't down to any one mod.</summary>
    public IReadOnlyList<string> ModIds { get; init; } = [];

    /// <summary>Gets the mods that clear it if they aren't turned on; empty when leaving a mod out would not.</summary>
    public IReadOnlyList<string> LeaveOutModIds { get; init; } = [];

    /// <summary>Gets what the window offers instead of the mod, such as the Fixes page for DXVK.</summary>
    public string? Instead { get; init; }
}
