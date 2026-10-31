namespace DAORework.Application.Fixes;

/// <summary>A fix DAORework applied, by its library id, and the files of it that were changed or removed outside DAORework since.</summary>
public sealed record AppliedFix(string LibraryId, string FixName, string? Version, IReadOnlyList<string> NotInEffect)
{
    /// <summary>Gets a value indicating whether every file the fix placed is still as DAORework left it.</summary>
    public bool InEffect => NotInEffect.Count == 0;
}
