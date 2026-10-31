using DAORework.Core.Mods.Library;

namespace DAORework.Core.Mods.Conflicts;

/// <summary>
/// A managed override file has the same name as a file the user placed in the override folder by hand. DAORework
/// never touches the user's file, and which copy the game loads is the game's choice, so this is a warning.
/// </summary>
public sealed record UnmanagedShadow(DeployedFile Managed, IReadOnlyList<string> UnmanagedPaths);
