using System.Text.RegularExpressions;

namespace DAORework.Core.Support;

/// <summary>
/// Removes the user's name from text before it leaves the machine in a support bundle: the folder after
/// <c>Users</c> or <c>home</c> in any path (plain or JSON-escaped), and the account name itself wherever it
/// appears as a whole word.
/// </summary>
public sealed partial class PathRedactor
{
    public const string Placeholder = "<user>";

    /// <summary>Names shorter than this are not redacted as bare words: too likely to match ordinary text.</summary>
    public const int MinimumBareNameLength = 3;

    private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(2);

    private readonly Regex? _bareName;

    public PathRedactor(string? userName)
    {
        _bareName = userName is { Length: >= MinimumBareNameLength }
            ? new Regex($@"(?<![\w]){Regex.Escape(userName)}(?![\w])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, _timeout)
            : null;
    }

    /// <summary>A redactor for the account whose profile folder is <paramref name="userProfile"/>.</summary>
    public static PathRedactor ForUserProfile(string? userProfile)
    {
        var trimmed = userProfile?.TrimEnd('\\', '/');
        var name = string.IsNullOrEmpty(trimmed) ? null : trimmed[(trimmed.LastIndexOfAny(['\\', '/']) + 1)..];
        return new PathRedactor(name);
    }

    public string Redact(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var redacted = UserSegment().Replace(text, "${prefix}" + Placeholder);
        return _bareName is null ? redacted : _bareName.Replace(redacted, Placeholder);
    }

    // "Users\" or "home/", including the doubled backslash of JSON text, then the folder name up to the next separator.
    [GeneratedRegex(@"(?<prefix>(?:^|[\\/:])(?:Users|home)(?:\\\\|\\|/))(?!<user>)[^\\/""\r\n\t]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 2000)]
    private static partial Regex UserSegment();
}
