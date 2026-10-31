using DAORework.Core.Mods.Library;

namespace DAORework.Core.Mods.Conflicts;

/// <summary>An override file has the same name as a file inside an add-in; the game uses the override copy.</summary>
public sealed record AddInFileOverridden(DeployedFile Override, DeployedFile AddInFile);
