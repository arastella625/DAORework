namespace DAORework.Core.Mods.Backups;

/// <summary>How a kind of change is worded: <c>Activated</c>/<c>Deactivated</c>/<c>Adopted</c> mods, or <c>Applied</c>/<c>Removed</c> fixes.</summary>
public sealed record ChangeWords(string Added, string Removed, string Moved, string Noun);
