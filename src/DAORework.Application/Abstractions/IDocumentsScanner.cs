using DAORework.Core.Common;
using DAORework.Core.Mods.Planning;

namespace DAORework.Application.Abstractions;

/// <summary>
/// Reads what the planner needs to know about a documents folder (ROADMAP P2-06): the hash of each named path that
/// exists, the override files DAORework did not place, and the UIDs <c>Settings\AddIns.xml</c> registers. Read only.
/// </summary>
public interface IDocumentsScanner
{
    Task<Result<DocumentsSnapshot>> ScanAsync(string documentsDirectory, IReadOnlyCollection<string> paths, CancellationToken cancellationToken = default);
}
