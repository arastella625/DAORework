using System.Text;

namespace DAORework.Infrastructure.Installations.Steam;

/// <summary>
/// A minimal reader for Valve's text KeyValues format, enough for <c>libraryfolders.vdf</c> and <c>appmanifest_*.acf</c>:
/// quoted keys and values, <c>{ }</c> blocks, <c>\\</c> and <c>\"</c> escapes, and <c>//</c> comments.
/// </summary>
public static class VdfReader
{
    /// <returns>The root node, whose children are the top-level keys.</returns>
    /// <exception cref="InvalidDataException">The text is not well-formed KeyValues.</exception>
    public static VdfNode Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var tokens = Tokenize(text);
        var position = 0;
        var root = new VdfNode();
        ReadPairs(tokens, ref position, root, expectClose: false);
        return root;
    }

    private static void ReadPairs(List<string> tokens, ref int position, VdfNode parent, bool expectClose)
    {
        while (position < tokens.Count)
        {
            var token = tokens[position++];
            if (token == "}")
            {
                if (expectClose)
                {
                    return;
                }

                throw new InvalidDataException("Unexpected '}' in KeyValues text.");
            }

            if (token == "{")
            {
                throw new InvalidDataException("A '{' must follow a key in KeyValues text.");
            }

            var key = Unquote(token);
            if (position >= tokens.Count)
            {
                throw new InvalidDataException($"Key '{key}' has no value.");
            }

            var next = tokens[position++];
            if (next == "{")
            {
                var child = new VdfNode();
                ReadPairs(tokens, ref position, child, expectClose: true);
                parent.Add(key, child);
            }
            else
            {
                parent.Add(key, new VdfNode(Unquote(next)));
            }
        }

        if (expectClose)
        {
            throw new InvalidDataException("Unterminated '{' in KeyValues text.");
        }
    }

    private static string Unquote(string token) =>
        token.Length >= 2 && token[0] == '"' ? token[1..^1] : token;

    private static List<string> Tokenize(string text)
    {
        var tokens = new List<string>();
        var index = 0;
        while (index < text.Length)
        {
            var c = text[index];
            if (char.IsWhiteSpace(c))
            {
                index++;
            }
            else if (c == '/' && index + 1 < text.Length && text[index + 1] == '/')
            {
                while (index < text.Length && text[index] != '\n')
                {
                    index++;
                }
            }
            else if (c is '{' or '}')
            {
                tokens.Add(c.ToString());
                index++;
            }
            else if (c == '"')
            {
                tokens.Add(ReadQuoted(text, ref index));
            }
            else
            {
                tokens.Add(ReadBare(text, ref index));
            }
        }

        return tokens;
    }

    private static string ReadQuoted(string text, ref int index)
    {
        var builder = new StringBuilder("\"");
        index++;
        while (index < text.Length && text[index] != '"')
        {
            if (text[index] == '\\' && index + 1 < text.Length)
            {
                index++;
            }

            builder.Append(text[index++]);
        }

        if (index >= text.Length)
        {
            throw new InvalidDataException("Unterminated string in KeyValues text.");
        }

        index++;
        return builder.Append('"').ToString();
    }

    private static string ReadBare(string text, ref int index)
    {
        var start = index;
        while (index < text.Length && !char.IsWhiteSpace(text[index]) && text[index] is not ('{' or '}' or '"'))
        {
            index++;
        }

        return text[start..index];
    }
}
