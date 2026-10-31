namespace DAORework.Core.Catalog;

/// <summary>
/// One way to recognize a <see cref="KnownItem"/>. <see cref="Value"/> is the UID, file name or install-relative path,
/// depending on <see cref="Kind"/>. <see cref="Marker"/>, for a game file, is a string the file must contain as a whole
/// NUL-terminated string; the string after it is read as the version. <see cref="Version"/>, for a download hash, is
/// the version of the item that download holds.
/// </summary>
public sealed record KnownItemFingerprint(FingerprintKind Kind, string Value, string? Marker = null, string? Version = null);
