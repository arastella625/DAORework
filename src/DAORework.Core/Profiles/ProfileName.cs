using System.Buffers;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;

namespace DAORework.Core.Profiles;

/// <summary>
/// A validated profile name that is also safe as a file name on every platform, so a
/// name never needs sanitizing on save or un-sanitizing on list (audit C7).
/// </summary>
public sealed record ProfileName
{
    public const int MaxLength = 64;

    private static readonly SearchValues<char> _forbiddenCharacters = SearchValues.Create("<>:\"/\\|?*");

    private static readonly HashSet<string> _reservedNames = new(
        ["CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"],
        StringComparer.OrdinalIgnoreCase);

    private ProfileName(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static ProfileName Default { get; } = new("Default");

    /// <summary>Validates a user-supplied name. Surrounding whitespace is trimmed.</summary>
    public static Result<ProfileName> Create(string? candidate)
    {
        var name = candidate?.Trim() ?? string.Empty;

        if (name.Length == 0)
        {
            return Result.Failure<ProfileName>(new Error(DiagnosticCodes.ProfileNameEmpty, "The profile name is empty."));
        }

        if (name.Length > MaxLength)
        {
            return Result.Failure<ProfileName>(new Error(DiagnosticCodes.ProfileNameTooLong, $"The profile name is longer than {MaxLength} characters."));
        }

        if (name.AsSpan().ContainsAny(_forbiddenCharacters) || name.Any(char.IsControl) || name[^1] == '.')
        {
            return Result.Failure<ProfileName>(new Error(
                DiagnosticCodes.ProfileNameInvalidCharacters,
                $"The profile name '{name}' contains a character that is not allowed.",
                "Use letters, digits, spaces, dashes and underscores, and do not end the name with a dot."));
        }

        var stem = name.Split('.', 2)[0];
        if (_reservedNames.Contains(stem))
        {
            return Result.Failure<ProfileName>(new Error(DiagnosticCodes.ProfileNameReserved, $"'{name}' is a reserved device name on Windows."));
        }

        return Result.Success(new ProfileName(name));
    }

    public override string ToString() => Value;
}
