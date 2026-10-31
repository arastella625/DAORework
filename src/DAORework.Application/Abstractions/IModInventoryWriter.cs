using DAORework.Core.Mods;

namespace DAORework.Application.Abstractions;

public interface IModInventoryWriter
{
    Task<string> WriteAsync(
        ModInventory report,
        CancellationToken cancellationToken = default);
}
