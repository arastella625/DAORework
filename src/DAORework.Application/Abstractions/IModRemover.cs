using DAORework.Core.Common;

namespace DAORework.Application.Abstractions;

/// <summary>Takes a mod out of the library for good (ADR-0039). Nothing else ever deletes from it.</summary>
public interface IModRemover
{
    /// <summary>Removes the mod's <c>mod.json</c>, then its files.</summary>
    Result Remove(string modId);
}
