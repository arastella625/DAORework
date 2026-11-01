namespace DAORework.Core.Mods.Combining;

/// <summary>One mod's copy of <c>chargenmorphcfg.xml</c>, as read from the library, and the mod's name for messages.</summary>
public sealed record ChargenMorphCopy(string ModId, string Xml, string? ModName = null);
