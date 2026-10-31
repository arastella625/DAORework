using DAORework.Core.Common;
using DAORework.Core.Diagnostics;

namespace DAORework.Application.Looks;

internal static class LookErrors
{
    public static Error NotFound(string lookId) => new(DiagnosticCodes.LookNotFound, $"There is no look '{lookId}'.");
}
