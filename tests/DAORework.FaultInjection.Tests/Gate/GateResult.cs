namespace DAORework.FaultInjection.Tests.Gate;

/// <summary>What a scenario's gate found: every failure, how many kills it made, and how many of them left a journal to settle.</summary>
internal sealed record GateResult(IReadOnlyList<string> Failures, int Kills, int JournalsSettled);
