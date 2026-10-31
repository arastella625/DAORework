using DAORework.Application.Abstractions;
using DAORework.Application.Diagnostics;
using DAORework.Application.Mods;
using DAORework.Application.Profiles;
using DAORework.Core.Common;
using DAORework.Core.Support;

namespace DAORework.Application.Support;

/// <summary>
/// One file a user can attach to a bug report: the profile, a fresh diagnostic run, the mod inventory (without
/// hashes, which are slow and say nothing to support) and the recent logs, with the user's name redacted.
/// </summary>
public sealed class ExportSupportBundle
{
    private readonly GetProfile _getProfile;
    private readonly RunDiagnostics _runDiagnostics;
    private readonly BuildModInventory _buildModInventory;
    private readonly ISupportBundleWriter _writer;
    private readonly IKnownFolders _knownFolders;

    public ExportSupportBundle(
        GetProfile getProfile,
        RunDiagnostics runDiagnostics,
        BuildModInventory buildModInventory,
        ISupportBundleWriter writer,
        IKnownFolders knownFolders)
    {
        _getProfile = getProfile;
        _runDiagnostics = runDiagnostics;
        _buildModInventory = buildModInventory;
        _writer = writer;
        _knownFolders = knownFolders;
    }

    public async Task<Result<string>> ExecuteAsync(ExportSupportBundleRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!(await _getProfile.ExecuteAsync(new GetProfileRequest(request.ProfileName), cancellationToken)).TryGetValue(out var profile, out var error))
        {
            return Result.Failure<string>(error);
        }

        var diagnostics = (await _runDiagnostics.ExecuteAsync(new RunDiagnosticsRequest(profile), cancellationToken)).Value;
        var inventory = await _buildModInventory.ExecuteAsync(new BuildModInventoryRequest(profile, IncludeHashes: false), cancellationToken);

        var bundle = new SupportBundle(
            profile,
            diagnostics,
            inventory.IsSuccess ? inventory.Value : null,
            inventory.Error,
            PathRedactor.ForUserProfile(_knownFolders.UserProfile));

        return Result.Success(await _writer.WriteAsync(bundle, cancellationToken));
    }
}
