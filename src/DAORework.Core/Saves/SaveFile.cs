namespace DAORework.Core.Saves;

/// <summary>A file to copy into a backup: where it is, and where it goes inside the zip (relative to the Characters folder).</summary>
public sealed record SaveFile(string SourcePath, string RelativePath, long SizeBytes);
