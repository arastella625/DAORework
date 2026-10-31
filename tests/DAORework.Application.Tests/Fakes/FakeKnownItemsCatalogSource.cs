using DAORework.Application.Abstractions;
using DAORework.Core.Catalog;
using DAORework.Core.Common;

namespace DAORework.Application.Tests.Fakes;

internal sealed class FakeKnownItemsCatalogSource : IKnownItemsCatalogSource
{
    private readonly Result<KnownItemsCatalog> _result;

    public FakeKnownItemsCatalogSource(Result<KnownItemsCatalog> result)
    {
        _result = result;
    }

    public FakeKnownItemsCatalogSource(IReadOnlyList<KnownItem> items, IReadOnlyList<KnownConflict>? conflicts = null)
        : this(KnownItemsCatalog.Validate(new KnownItemsCatalog(KnownItemsCatalog.CurrentSchemaVersion, items, conflicts ?? [])))
    {
    }

    public Result<KnownItemsCatalog> Load() => _result;
}
