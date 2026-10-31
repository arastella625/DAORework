namespace DAORework.Application.Abstractions;

/// <summary>What to start. Arguments are passed one per entry, so quoting is handled by the adapter (SDD §8.6).</summary>
public sealed record ProcessStartRequest(string FileName, string? WorkingDirectory, IReadOnlyList<string> Arguments);
