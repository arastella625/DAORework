namespace DAORework.Core.Launching;

/// <summary>A started game process. Failures are <see cref="Common.Error"/>s on the surrounding <see cref="Common.Result{T}"/>.</summary>
public sealed record LaunchResult(int ProcessId, string ExecutablePath);
