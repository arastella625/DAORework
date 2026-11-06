using DAORework.Core.Mods.Library;
using DAORework.Core.Mods.Planning;

namespace DAORework.Application.Mods.Library;

/// <summary>What keeping one copy of everything would do, and the one plan that makes every swap, if there are any.</summary>
public sealed record KeepEachPreview(KeepEachPlan Plan, ModActivationPlan? Swap);
