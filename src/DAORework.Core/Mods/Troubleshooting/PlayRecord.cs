namespace DAORework.Core.Mods.Troubleshooting;

/// <summary>One game started from the window: when, and exactly which mods were on, in their order (ADR-0043).</summary>
public sealed record PlayRecord(DateTimeOffset PlayedAt, IReadOnlyList<string> ModIds);
