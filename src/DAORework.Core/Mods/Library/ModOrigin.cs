namespace DAORework.Core.Mods.Library;

/// <summary>
/// Where a library mod came from (ADR-0015). <see cref="AdoptedPath"/> is set only for an adopted mod: the folder its
/// files were adopted from, relative to the documents folder and with <c>/</c>, such as <c>AddIns/Extra_Dog_Slot</c>.
/// </summary>
public sealed record ModOrigin(ModOriginKind Kind, string? AdoptedPath = null)
{
    public static ModOrigin Download { get; } = new(ModOriginKind.Download);
}
