using DAORework.Core.Common;

namespace DAORework.Application.Abstractions;

/// <summary>Lists the resources inside a library mod's <c>.erf</c> (ROADMAP P3-07). Only the archive's table of contents is read.</summary>
public interface IErfContentsReader
{
    Result<IReadOnlyList<string>> ReadNames(string modId, string relativePath);
}
