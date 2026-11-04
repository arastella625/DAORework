using System.Text.Json;
using DAORework.Application.Profiles;
using Microsoft.Extensions.Logging;

namespace DAORework.Application.Documents;

/// <summary>The documents folders the saved profiles name: places the user chose, or that detection found before (ADR-0032).</summary>
public sealed class ProfileDocumentsFolders
{
    private readonly IProfileRepository _profiles;
    private readonly ILogger<ProfileDocumentsFolders> _logger;

    public ProfileDocumentsFolders(IProfileRepository profiles, ILogger<ProfileDocumentsFolders> logger)
    {
        _profiles = profiles;
        _logger = logger;
    }

    public async Task<IReadOnlyList<string>> ReadAsync(CancellationToken cancellationToken = default)
    {
        var folders = new List<string>();
        foreach (var name in await NamesAsync(cancellationToken))
        {
            if (await NamedByAsync(name, cancellationToken) is { } folder)
            {
                folders.Add(folder);
            }
        }

        return folders;
    }

    private async Task<IReadOnlyList<string>> NamesAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await _profiles.ListNamesAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Detection has other places to look, and the folders Windows reports need no profiles.
            _logger.LogWarning(exception, "Could not list the profiles while looking for the documents folder.");
            return [];
        }
    }

    private async Task<string?> NamedByAsync(string profileName, CancellationToken cancellationToken)
    {
        try
        {
            var folder = (await _profiles.LoadAsync(profileName, cancellationToken))?.UserDocumentsDirectory;
            return string.IsNullOrWhiteSpace(folder) ? null : folder;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException or JsonException)
        {
            // JsonException too: a file that parses but holds a value of the wrong type fails after the parse.
            _logger.LogWarning(exception, "Profile {Profile} could not be read while looking for the documents folder.", profileName);
            return null;
        }
    }
}
