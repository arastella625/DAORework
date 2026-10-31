using DAORework.Core.Common;

namespace DAORework.Application.Abstractions;

/// <summary>
/// Copies a picture the user picked into a look's folder (ADR-0017): decoded, refused if it is not a PNG, JPEG or WebP
/// or is too large, scaled down to at most 3840 px on its long edge, and measured. The original is never changed.
/// </summary>
public interface IBackgroundImageImporter
{
    /// <returns>The copy's file name and its brightest color, or <c>DAO_LOOK_IMAGE_UNREADABLE</c> or <c>DAO_LOOK_IMAGE_TOO_LARGE</c>.</returns>
    Task<Result<ImportedPicture>> ImportAsync(string sourcePath, string lookId, CancellationToken cancellationToken = default);

    /// <returns>The bytes of a look's copy of its picture, to show it, or <c>DAO_LOOK_IMAGE_UNREADABLE</c>.</returns>
    Task<Result<byte[]>> ReadAsync(string lookId, string fileName, CancellationToken cancellationToken = default);

    /// <summary>Removes a look's picture. A picture that is not there is already removed.</summary>
    Task RemoveAsync(string lookId, string fileName, CancellationToken cancellationToken = default);
}
