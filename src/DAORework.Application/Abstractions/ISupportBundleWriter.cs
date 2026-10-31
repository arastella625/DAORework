using DAORework.Core.Support;

namespace DAORework.Application.Abstractions;

/// <summary>Writes a support bundle, with the recent logs added and everything redacted, as one zip in the reports folder.</summary>
public interface ISupportBundleWriter
{
    /// <returns>The path of the zip.</returns>
    Task<string> WriteAsync(SupportBundle bundle, CancellationToken cancellationToken = default);
}
