namespace DAORework.Core.Mods.Planning;

/// <summary>A reason the plan cannot be applied as it is, with the path or UID it concerns and what to tell the user.</summary>
public sealed record PlanBlocker(PlanBlockerKind Kind, string Subject, string Message);
