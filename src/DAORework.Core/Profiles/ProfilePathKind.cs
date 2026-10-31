namespace DAORework.Core.Profiles;

/// <summary>Which path on a <see cref="RuntimeProfile"/> an update targets.</summary>
public enum ProfilePathKind
{
    Install = 0,
    Game = 1,
    Launcher = 2,
    Documents = 3,
}
