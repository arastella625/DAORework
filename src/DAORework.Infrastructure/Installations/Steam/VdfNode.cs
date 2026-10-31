namespace DAORework.Infrastructure.Installations.Steam;

/// <summary>One node of a Valve KeyValues ("VDF") document: either a string value or a set of named children.</summary>
public sealed class VdfNode
{
    private readonly Dictionary<string, VdfNode> _children = new(StringComparer.OrdinalIgnoreCase);

    public VdfNode(string? value = null)
    {
        Value = value;
    }

    public string? Value { get; }

    public IReadOnlyDictionary<string, VdfNode> Children => _children;

    /// <returns>The child's string value, or null when there is no such child or it is not a value.</returns>
    public string? GetString(string key) => _children.TryGetValue(key, out var child) ? child.Value : null;

    public VdfNode? GetChild(string key) => _children.TryGetValue(key, out var child) ? child : null;

    internal void Add(string key, VdfNode child) => _children[key] = child;
}
