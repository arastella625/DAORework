namespace DAORework.Core.Catalog;

/// <summary>Another catalog entry an item needs to work, with a link to where its author says so (ADR-0033 §5).</summary>
/// <param name="Item">The needed entry's id.</param>
/// <param name="Source">An https link to the author's own words.</param>
public sealed record KnownNeed(string Item, string Source);
