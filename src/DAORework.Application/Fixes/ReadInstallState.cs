using DAORework.Application.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Fixes;
using DAORework.Core.Mods.Planning;

namespace DAORework.Application.Fixes;

/// <summary>
/// What the install plan builder needs from the install folder (ADR-0013): the current hash of each path in play, and
/// why any of them can't be written now. Shared by fixes and by mods that run inside the game (ADR-0033 §4). Reads only.
/// </summary>
public sealed class ReadInstallState
{
    private readonly IInstallFiles _files;

    public ReadInstallState(IInstallFiles files)
    {
        _files = files;
    }

    public async Task<Result<InstallState>> ExecuteAsync(string install, DeploymentRecord record, IReadOnlyList<string> paths, IReadOnlySet<string> replaceable, CancellationToken cancellationToken = default)
    {
        if (!(await _files.HashAsync(install, paths, cancellationToken)).TryGetValue(out var hashes, out var error))
        {
            return Result.Failure<InstallState>(error);
        }

        var problems = await _files.CheckWritableAsync(install, paths, cancellationToken);
        return Result.Success(new InstallState(record, hashes, replaceable, problems.Select(Blocker).ToList()));
    }

    private static PlanBlocker Blocker(Error error) =>
        new(error.Code == DiagnosticCodes.FixFileInUse ? PlanBlockerKind.FileInUse : PlanBlockerKind.InstallNotWritable, string.Empty, $"{error.Message} {error.Recommendation}".TrimEnd());
}
