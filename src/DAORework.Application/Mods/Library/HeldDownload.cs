using DAORework.Core.Mods.Adoption;

namespace DAORework.Application.Mods.Library;

/// <summary>A download that looks already held (ADR-0039), with the name it would be added under.</summary>
public sealed record HeldDownload(string Path, string Name, HeldElsewhere Held);
