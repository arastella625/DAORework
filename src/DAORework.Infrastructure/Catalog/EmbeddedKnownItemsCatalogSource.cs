using System.Text.Json;
using DAORework.Application.Abstractions;
using DAORework.Core.Catalog;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Infrastructure.Persistence;

namespace DAORework.Infrastructure.Catalog;

/// <summary>
/// The known-items catalog embedded in this assembly from <c>catalog/known-items.json</c> (ADR-0011). Parsed and
/// validated once; a broken catalog is a failed result, never an exception at the user.
/// </summary>
public sealed class EmbeddedKnownItemsCatalogSource : IKnownItemsCatalogSource
{
    /// <summary>The manifest resource name set in the project file.</summary>
    public const string ResourceName = "DAORework.KnownItems.json";

    private readonly Lazy<Result<KnownItemsCatalog>> _catalog = new(LoadEmbedded);

    public Result<KnownItemsCatalog> Load() => _catalog.Value;

    /// <summary>Parses and validates a catalog document; used for the embedded copy and by tests.</summary>
    public static Result<KnownItemsCatalog> Parse(Stream json)
    {
        ArgumentNullException.ThrowIfNull(json);
        try
        {
            return KnownItemsCatalog.Validate(JsonSerializer.Deserialize<KnownItemsCatalog>(json, JsonDefaults.Options));
        }
        catch (JsonException exception)
        {
            return Result.Failure<KnownItemsCatalog>(new Error(DiagnosticCodes.CatalogInvalid, $"The known-items catalog is not valid JSON: {exception.Message}"));
        }
    }

    private static Result<KnownItemsCatalog> LoadEmbedded()
    {
        using var stream = typeof(EmbeddedKnownItemsCatalogSource).Assembly.GetManifestResourceStream(ResourceName);
        return stream is null
            ? Result.Failure<KnownItemsCatalog>(new Error(DiagnosticCodes.CatalogInvalid, $"The known-items catalog resource {ResourceName} is missing from this build."))
            : Parse(stream);
    }
}
