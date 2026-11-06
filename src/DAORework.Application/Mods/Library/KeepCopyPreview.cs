using DAORework.Core.Mods.Library;
using DAORework.Core.Mods.Planning;

namespace DAORework.Application.Mods.Library;

/// <summary>What keeping one copy would do, and the plan that swaps a copy that is on, if one is.</summary>
public sealed record KeepCopyPreview(KeepCopyPlan Plan, ModActivationPlan? Swap);
