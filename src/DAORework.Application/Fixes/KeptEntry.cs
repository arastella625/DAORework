using DAORework.Core.Fixes;

namespace DAORework.Application.Fixes;

/// <summary>Another entry stored with a fix so that removing it can put back what it replaced, such as a DXVK DLL placed by hand.</summary>
public sealed record KeptEntry(FixEntry Entry, IReadOnlyDictionary<string, byte[]> Contents);
