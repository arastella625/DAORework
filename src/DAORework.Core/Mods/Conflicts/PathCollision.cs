using DAORework.Core.Mods.Library;

namespace DAORework.Core.Mods.Conflicts;

/// <summary>
/// Two mods would write the same exact file outside the override folder, such as the same add-in folder twice. Unlike
/// an override conflict this cannot be resolved by order, so a plan that contains one is refused.
/// </summary>
public sealed record PathCollision(string TargetPath, IReadOnlyList<DeployedFile> Files);
