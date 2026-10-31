using DAORework.Core.Common;
using DAORework.Core.Looks;

namespace DAORework.Application.Looks;

/// <summary>
/// A look ready to show: the look, its picture's bytes when it has one, and why the picture is missing when it should
/// be there. A missing picture does not stop the look: it is shown without it.
/// </summary>
public sealed record LoadedLook(Look Look, byte[]? Picture, Error? PictureError);
