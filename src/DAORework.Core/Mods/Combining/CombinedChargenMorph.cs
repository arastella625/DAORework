namespace DAORework.Core.Mods.Combining;

/// <summary>The combined <c>chargenmorphcfg.xml</c> (ADR-0025): its text, how many entries it lists, and the mods it came from, in load order.</summary>
public sealed record CombinedChargenMorph(string Xml, int EntryCount, IReadOnlyList<string> ModIds);
