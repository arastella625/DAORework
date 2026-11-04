using DAORework.Application.Profiles;

namespace DAORework.Application.Documents;

/// <summary>The documents folders the saved profiles name: places the user chose, or that detection found before (ADR-0031).</summary>
public sealed class ProfileDocumentsFolders
{
    private readonly IProfileRepository _profiles;

    public ProfileDocumentsFolders(IProfileRepository profiles)
    {
        _profiles = profiles;
    }

    public async Task<IReadOnlyList<string>> ReadAsync(CancellationToken cancellationToken = default)
    {
        var folders = new List<string>();
        foreach (var name in await _profiles.ListNamesAsync(cancellationToken))
        {
            if (await NamedByAsync(name, cancellationToken) is { } folder)
            {
                folders.Add(folder);
            }
        }

        return folders;
    }

    private async Task<string?> NamedByAsync(string profileName, CancellationToken cancellationToken)
    {
        try
        {
            var folder = (await _profiles.LoadAsync(profileName, cancellationToken))?.UserDocumentsDirectory;
            return string.IsNullOrWhiteSpace(folder) ? null : folder;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            // A profile that cannot be read names no folder. The commands that use that profile report the problem.
            return null;
        }
    }
}
