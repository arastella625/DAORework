using System.Globalization;
using System.IO.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Formats;
using DAORework.Core.Profiles;

namespace DAORework.Application.Diagnostics.Rules;

/// <summary>The game's settings file: present and readable, with the resolution reported and sanity-checked when it is there.</summary>
public sealed class ConfigRule : IDiagnosticRule
{
    private const int MinimumDimension = 640;
    private const int MaximumDimension = 16384;

    private readonly IFileSystem _fileSystem;

    public ConfigRule(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public Task<IReadOnlyList<DiagnosticIssue>> EvaluateAsync(RuntimeProfile profile, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var documents = profile.UserDocumentsDirectory;
        if (string.IsNullOrWhiteSpace(documents) || !_fileSystem.Directory.Exists(documents))
        {
            return Task.FromResult<IReadOnlyList<DiagnosticIssue>>([]);
        }

        var path = GamePaths.SettingsFile(documents);
        if (!_fileSystem.File.Exists(path))
        {
            return Task.FromResult<IReadOnlyList<DiagnosticIssue>>(
                [new DiagnosticIssue(DiagnosticSeverity.Info, DiagnosticCodes.ConfigNotFound, $"No settings file yet at {path}. The game writes it on first launch.")]);
        }

        return Task.FromResult<IReadOnlyList<DiagnosticIssue>>(Inspect(path));
    }

    private List<DiagnosticIssue> Inspect(string path)
    {
        IniDocument settings;
        try
        {
            settings = IniDocument.Parse(_fileSystem.File.ReadAllText(path));
        }
        catch (IOException exception)
        {
            return [new DiagnosticIssue(DiagnosticSeverity.Warning, DiagnosticCodes.ConfigUnreadable, $"Could not read {path}: {exception.Message}")];
        }
        catch (UnauthorizedAccessException exception)
        {
            return [new DiagnosticIssue(DiagnosticSeverity.Warning, DiagnosticCodes.ConfigUnreadable, $"Could not read {path}: {exception.Message}")];
        }

        var width = ReadDimension(settings, "Width", "ResolutionWidth");
        var height = ReadDimension(settings, "Height", "ResolutionHeight");
        var resolution = width is null || height is null ? "resolution not recorded" : $"resolution {width}x{height}";
        var issues = new List<DiagnosticIssue>
        {
            new(DiagnosticSeverity.Info, DiagnosticCodes.ConfigFound, $"Settings file found ({settings.Sections.Count} sections, {resolution})."),
        };

        if ((width is not null && !IsSane(width.Value)) || (height is not null && !IsSane(height.Value)))
        {
            issues.Add(new DiagnosticIssue(
                DiagnosticSeverity.Warning,
                DiagnosticCodes.ConfigResolutionSuspicious,
                $"The settings file records an unusual resolution: {width}x{height}.",
                "If the game starts with a black or tiny window, delete the Settings folder and let the game recreate it."));
        }

        return issues;
    }

    private static int? ReadDimension(IniDocument settings, params string[] keys) =>
        keys.Select(settings.FindAnywhere)
            .Select(value => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) ? number : (int?)null)
            .FirstOrDefault(number => number is not null);

    private static bool IsSane(int dimension) => dimension is >= MinimumDimension and <= MaximumDimension;
}
