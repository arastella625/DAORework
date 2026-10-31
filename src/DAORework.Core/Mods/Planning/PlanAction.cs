namespace DAORework.Core.Mods.Planning;

public enum PlanAction
{
    /// <summary>Copy a library file to a path where nothing exists.</summary>
    Copy,

    /// <summary>Overwrite a file DAORework placed, still unchanged, with a different library file.</summary>
    Replace,

    /// <summary>Remove a file DAORework placed, still unchanged, that no active mod deploys any more.</summary>
    Delete,

    /// <summary>
    /// Move a hand-installed file DAORework is adopting from <see cref="PlanOperation.MovedFrom"/> to the target, a
    /// rename inside the documents folder, while it still hashes to the expected SHA-256 (ADR-0015).
    /// </summary>
    Move,
}
