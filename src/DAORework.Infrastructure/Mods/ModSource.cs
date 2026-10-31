namespace DAORework.Infrastructure.Mods;

/// <summary>A download being imported (P2-07): its display name, the SHA-256 that identifies it, and its files by <c>/</c> path.</summary>
internal abstract class ModSource : IDisposable
{
    protected ModSource(string name, string sha256)
    {
        Name = name;
        Sha256 = sha256;
    }

    public string Name { get; }

    public string Sha256 { get; }

    public abstract IReadOnlyList<string> Files { get; }

    /// <summary>Gets the total size of the files once extracted.</summary>
    public abstract long TotalBytes { get; }

    public abstract Stream Open(string file);

    /// <summary>Hands each of <see cref="Files"/> to <paramref name="copy"/> once, in whatever order the source reads fastest.</summary>
    public virtual async Task CopyEachAsync(Func<string, Stream, Task> copy, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(copy);
        foreach (var file in Files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await using var input = Open(file);
            await copy(file, input);
        }
    }

    public virtual void Dispose()
    {
    }
}
