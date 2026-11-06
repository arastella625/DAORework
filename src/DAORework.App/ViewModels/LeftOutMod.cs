namespace DAORework.App.ViewModels;

/// <summary>A mod a batch went on without (P2.6-53).</summary>
/// <param name="Title">Its name, as the player knows it.</param>
/// <param name="Why">What stopped it, in a sentence.</param>
internal sealed record LeftOutMod(string Title, string Why);
