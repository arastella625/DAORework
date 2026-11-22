using DAORework.Core.Common;

namespace DAORework.Application.Abstractions;

/// <summary>
/// The game's own <c>chargenmorphcfg.xml</c>, read from the player's install, never written (ADR-0025, amended
/// 2026-11-22): the list every mod adds to, so what a mod adds can be counted (ADR-0050 §4).
/// </summary>
public interface IGameChargenMorphSource
{
    /// <summary>The file's text, or null when the install has no copy DAORework can read as plain data.</summary>
    Task<Result<string?>> ReadAsync(string installDirectory, CancellationToken cancellationToken);
}
