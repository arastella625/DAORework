namespace DAORework.Core.Mods.Handover;

/// <summary>One Vortex mod copied into the library (ADR-0028 §3): its Vortex folder name and the library mod that holds it.</summary>
public sealed record VortexCopy(string VortexName, string ModId);
