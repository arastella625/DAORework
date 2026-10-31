namespace DAORework.Core.Formats;

/// <summary>A minimal reader for the game's <c>DragonAge.ini</c>: <c>[Section]</c> headers, <c>Key=Value</c> lines, <c>;</c> comments.</summary>
public sealed class IniDocument
{
    private readonly Dictionary<string, Dictionary<string, string>> _sections = new(StringComparer.OrdinalIgnoreCase);

    private IniDocument()
    {
    }

    public IReadOnlyCollection<string> Sections => _sections.Keys;

    public static IniDocument Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var document = new IniDocument();
        var section = string.Empty;

        foreach (var rawLine in text.Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line[0] is ';' or '#')
            {
                continue;
            }

            if (line[0] == '[' && line[^1] == ']')
            {
                section = line[1..^1].Trim();
                document.Section(section);
                continue;
            }

            var separator = line.IndexOf('=', StringComparison.Ordinal);
            if (separator > 0)
            {
                document.Section(section)[line[..separator].Trim()] = line[(separator + 1)..].Trim();
            }
        }

        return document;
    }

    /// <returns>The value, or null when the section or key is absent.</returns>
    public string? Get(string section, string key) =>
        _sections.TryGetValue(section, out var values) && values.TryGetValue(key, out var value) ? value : null;

    /// <summary>The first value of <paramref name="key"/> in any section, for files whose section names vary between game versions.</summary>
    public string? FindAnywhere(string key) =>
        _sections.Values.Select(values => values.TryGetValue(key, out var value) ? value : null).FirstOrDefault(v => v is not null);

    private Dictionary<string, string> Section(string name)
    {
        if (!_sections.TryGetValue(name, out var values))
        {
            values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _sections[name] = values;
        }

        return values;
    }
}
