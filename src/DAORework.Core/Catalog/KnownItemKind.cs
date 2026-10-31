namespace DAORework.Core.Catalog;

public enum KnownItemKind
{
    Fix,
    Mod,
    Tool,

    /// <summary>BioWare's own DLC. Never offered for adoption and never touched (ADR-0015).</summary>
    Official,
}
