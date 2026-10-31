using DAORework.Application.Documents;
using DAORework.Application.Installations;
using DAORework.Core.Common;
using DAORework.Core.Profiles;

namespace DAORework.Application.Profiles;

/// <summary>Creates (or replaces) a profile, pre-filled from the best detected install and the recommended documents folder.</summary>
public sealed class CreateProfile
{
    private readonly IProfileRepository _profiles;
    private readonly DetectInstallations _detectInstallations;
    private readonly DetectDocumentsFolders _detectDocumentsFolders;

    public CreateProfile(IProfileRepository profiles, DetectInstallations detectInstallations, DetectDocumentsFolders detectDocumentsFolders)
    {
        _profiles = profiles;
        _detectInstallations = detectInstallations;
        _detectDocumentsFolders = detectDocumentsFolders;
    }

    public async Task<Result<RuntimeProfile>> ExecuteAsync(CreateProfileRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!ProfileName.Create(request.Name).TryGetValue(out var name, out var nameError))
        {
            return Result.Failure<RuntimeProfile>(nameError);
        }

        var installations = (await _detectInstallations.ExecuteAsync(new DetectInstallationsRequest(), cancellationToken)).Value;
        var detected = installations.Count > 0 ? installations[0] : null;
        var documents = (await _detectDocumentsFolders.ExecuteAsync(new DetectDocumentsFoldersRequest(), cancellationToken)).Value;

        var profile = new RuntimeProfile
        {
            Name = name.Value,
            InstallDirectory = detected?.InstallDirectory,
            GameExecutablePath = detected?.GameExecutablePath,
            LauncherExecutablePath = detected?.LauncherExecutablePath,
            UserDocumentsDirectory = documents.Recommended.Path,
        };

        await _profiles.SaveAsync(profile, cancellationToken);
        return Result.Success(profile);
    }
}
