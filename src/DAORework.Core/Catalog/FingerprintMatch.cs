namespace DAORework.Core.Catalog;

/// <summary>
/// Where a fingerprint was found, as the user would recognize it, with the version read from the file if any.
/// <see cref="WrongArchitecture"/> describes a file the 32-bit game cannot load, such as DXVK's x64 <c>d3d9.dll</c>.
/// </summary>
public sealed record FingerprintMatch(string Location, string? Version = null, string? WrongArchitecture = null);
