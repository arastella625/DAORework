namespace DAORework.Formats.Erf;

/// <summary>One file in an ERF archive, as its table of contents gives it. For version 2.0, the two sizes are the same.</summary>
public sealed record ErfEntry(string Name, uint Offset, uint PackedSize, uint UnpackedSize);
