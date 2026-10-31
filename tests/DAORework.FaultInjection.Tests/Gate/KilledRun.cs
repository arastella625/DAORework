namespace DAORework.FaultInjection.Tests.Gate;

/// <summary>How a child CLI ended: killed or not, before which write, and how many writes it made when it was not.</summary>
internal sealed record KilledRun(int ExitCode, bool Killed, string KilledBefore, int Writes, string Output);
