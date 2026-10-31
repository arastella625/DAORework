namespace DAORework.Core.Mods.Planning;

public enum PlanBlockerKind
{
    /// <summary>Two mods would write the same file outside the override folder.</summary>
    PathCollision,

    /// <summary>A file DAORework placed was changed afterwards; it is the user's call whether to replace it.</summary>
    ChangedSinceDeployed,

    /// <summary>A file DAORework did not place is where a mod's file would go; DAORework never overwrites it.</summary>
    UnmanagedFileInTheWay,

    /// <summary>An add-in's UID is already registered in AddIns.xml by something other than DAORework.</summary>
    AddInAlreadyRegistered,

    /// <summary>The file a fix replaced is not in the library any more, so removing the fix cannot put it back.</summary>
    FixOriginalMissing,

    /// <summary>DAORework cannot write to the install folder without administrator rights (ADR-0013).</summary>
    InstallNotWritable,

    /// <summary>A file the plan changes is open in another program, usually the running game.</summary>
    FileInUse,
}
