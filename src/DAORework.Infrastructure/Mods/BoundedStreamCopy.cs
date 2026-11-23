namespace DAORework.Infrastructure.Mods;

internal static class BoundedStreamCopy
{
    // Will read at most one extra buffer size to detect maxBytes was exceeded but will never write it to destination.
    public static async Task CopyAsync(
        Stream source,
        Stream destination,
        long maxBytes,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxBytes);

        // 80KiB buffer for chunk size
        var buffer = new byte[81920];
        long copied = 0;

        while (true)
        {
            var read = await source.ReadAsync(buffer.AsMemory(), cancellationToken);

            if (read == 0)
            {
                return;
            }

            // Checking to avoid overflow
            if (read > maxBytes - copied)
            {
                throw new InvalidDataException($"The embedded archive exceeds the {maxBytes}-byte limit.");
            }

            await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);

            copied += read;
        }
    }
}
