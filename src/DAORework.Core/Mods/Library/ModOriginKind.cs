namespace DAORework.Core.Mods.Library;

/// <summary>How a mod came into the library (ADR-0015).</summary>
public enum ModOriginKind
{
    /// <summary>Imported from a download the user supplied (<c>mods import</c>).</summary>
    Download,

    /// <summary>Adopted from files the user had installed by hand.</summary>
    Adopted,
}
