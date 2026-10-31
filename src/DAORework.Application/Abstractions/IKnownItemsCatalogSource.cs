using DAORework.Core.Catalog;
using DAORework.Core.Common;

namespace DAORework.Application.Abstractions;

/// <summary>Where the known-items catalog comes from (ADR-0011): the copy embedded in the release. The result is validated.</summary>
public interface IKnownItemsCatalogSource
{
    Result<KnownItemsCatalog> Load();
}
