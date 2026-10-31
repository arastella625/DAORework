namespace DAORework.Core.Launching;

/// <summary>
/// A started game. <see cref="ProcessId"/> is null when Steam was asked to start it (ADR-0022), and
/// <see cref="ExecutablePath"/> is then the <c>steam://</c> link. Failures are <see cref="Common.Error"/>s on the
/// surrounding <see cref="Common.Result{T}"/>.
/// </summary>
public sealed record LaunchResult(int? ProcessId, string ExecutablePath)
{
    public bool StartedBySteam => ProcessId is null;
}
