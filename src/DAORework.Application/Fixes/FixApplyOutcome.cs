using DAORework.Core.Mods.Apply;

namespace DAORework.Application.Fixes;

/// <summary>What applying a fix plan did: the executor's report, or null when nothing on disk had to change and only the record was brought up to date.</summary>
public sealed record FixApplyOutcome(ApplyReport? Report);
