namespace DAORework.Core.Mods.Library;

/// <summary>One file of a mod that runs inside the game, and where it goes: relative to the install folder, under <c>bin_ship</c> (ADR-0033 §1).</summary>
public sealed record GameProgramFile(LibraryModFile File, string TargetPath);
