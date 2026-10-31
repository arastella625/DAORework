using DAORework.Core.Looks;

namespace DAORework.Application.Abstractions;

/// <summary>A picture copied into a look's folder: its file name there, its size after scaling, and its brightest part.</summary>
public sealed record ImportedPicture(string FileName, int Width, int Height, LookColor Brightest);
