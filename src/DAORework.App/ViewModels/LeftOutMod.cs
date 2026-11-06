namespace DAORework.App.ViewModels;

/// <summary>A mod a batch went on without (P2.6-53).</summary>
/// <param name="Title">Its name, as the player knows it.</param>
/// <param name="Why">What stopped it, in a sentence a player can read.</param>
/// <param name="Detail">The technical reason, for <i>Show details</i> (docs/VOICE.md rule 2).</param>
internal sealed record LeftOutMod(string Title, string Why, string? Detail = null);
