using DAORework.Core.Common;

namespace DAORework.Application.Abstractions;

/// <summary>Opens a link with the program Windows has for it, such as a <c>steam://</c> link (ADR-0022).</summary>
public interface IUriLauncher
{
    Result Open(Uri uri);
}
