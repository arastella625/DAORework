using DAORework.Core.Mods;

namespace DAORework.Application.Abstractions;

public interface IDaoModProfileService
{
    Task<IReadOnlyList<DaoModProfile>> ListAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(DaoModProfile profile, CancellationToken cancellationToken = default);
}
