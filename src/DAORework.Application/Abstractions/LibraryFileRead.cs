namespace DAORework.Application.Abstractions;

/// <summary>A library file as read now: its full path, for the antivirus; its SHA-256; and up to the bytes asked for from its start.</summary>
public sealed record LibraryFileRead(string Path, string Sha256, byte[] Start);
