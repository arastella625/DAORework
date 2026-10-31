using System.Text.RegularExpressions;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;

namespace DAORework.Core.Looks;

/// <summary>What a look's name and id may be. An id is also a folder name, so only a strict form is accepted.</summary>
public static partial class LookNames
{
    public static Result<string> ValidName(string? name)
    {
        var trimmed = name?.Trim() ?? string.Empty;
        return trimmed.Length is 0 or > Look.MaxNameLength
            ? Result.Failure<string>(new Error(DiagnosticCodes.LookNameInvalid, $"A look's name is 1 to {Look.MaxNameLength} characters."))
            : Result.Success(trimmed);
    }

    public static bool IsValidId(string? id) => id is not null && IdFormat().IsMatch(id);

    public static string NewId() => Look.IdPrefix + Guid.NewGuid().ToString("N")[..12];

    [GeneratedRegex("^look-[a-z0-9]{1,32}$", RegexOptions.CultureInvariant)]
    private static partial Regex IdFormat();
}
