namespace DAORework.Core.Mods.Library;

/// <summary>What an archive holds; for <see cref="ArchiveShape.WrappedAddIn"/>, <see cref="InnerDazip"/> is the entry of the <c>.dazip</c> inside.</summary>
public sealed record ArchiveClassification(ArchiveShape Shape, string? InnerDazip = null);
