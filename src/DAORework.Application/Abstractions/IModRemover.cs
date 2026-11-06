using DAORework.Core.Common;

namespace DAORework.Application.Abstractions;

/// <summary>Takes a mod out of the library (ADR-0039), to the Recycle Bin where there is one. Nothing else ever deletes from it.</summary>
public interface IModRemover
{
    /// <summary>Gets a value indicating whether what is removed can still be put back from the Recycle Bin.</summary>
    bool GoesToRecycleBin { get; }

    /// <summary>Removes the mod's <c>mod.json</c>, then its files.</summary>
    Result Remove(string modId);
}
