namespace DAORework.Core.Fixes;

/// <summary>
/// The patched image, whether the flag was set already (then <see cref="Image"/> is the original, unchanged), and
/// whether the original carries an Authenticode signature the patch invalidates.
/// </summary>
public sealed record LargeAddressAwarePatchResult(byte[] Image, bool WasAlreadySet, bool IsSigned);
