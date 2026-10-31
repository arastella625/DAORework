using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods;
using DAORework.Core.Profiles;

namespace DAORework.Core.Support;

/// <summary>
/// Everything a support bundle holds besides the logs, which the writer adds from the app-data folder. Either
/// <see cref="Inventory"/> or <see cref="InventoryError"/> is set: a failed inventory is itself useful to support.
/// </summary>
public sealed record SupportBundle(
    RuntimeProfile Profile,
    DiagnosticReport Diagnostics,
    ModInventory? Inventory,
    Error? InventoryError,
    PathRedactor Redactor)
{
    /// <summary>The schema of the bundle's <c>manifest.json</c> (SDD §8.4).</summary>
    public const int CurrentSchemaVersion = 1;
}
