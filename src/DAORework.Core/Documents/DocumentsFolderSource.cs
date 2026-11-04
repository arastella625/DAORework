namespace DAORework.Core.Documents;

/// <summary>Where a candidate Dragon Age documents folder came from.</summary>
public enum DocumentsFolderSource
{
    /// <summary>The Documents folder Windows reports, which is the one the game uses. Under OneDrive Known Folder Move this already points into OneDrive.</summary>
    KnownFolder = 0,

    /// <summary>The classic <c>%UserProfile%\Documents</c>, where saves from before a OneDrive redirection may still be.</summary>
    UserProfile = 1,

    /// <summary>The <c>%OneDrive%\Documents</c> folder, where a redirection puts them.</summary>
    OneDrive = 2,

    /// <summary>A folder a saved profile names: one the user chose, or that detection found before.</summary>
    Profile = 3,

    /// <summary>A folder found by searching the drives, when no other source found one (ADR-0032).</summary>
    DriveSearch = 4,

    /// <summary>A folder inside the game's Proton prefix, on Linux (ADR-0031).</summary>
    ProtonPrefix = 5,
}
