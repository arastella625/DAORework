namespace DAORework.Core.Installations;

/// <summary>An install as it was found at detection time. The executable paths are set only when the file existed then.</summary>
public sealed class Installation
{
    public required string InstallDirectory { get; init; }

    public string? GameExecutablePath { get; init; }

    public string? LauncherExecutablePath { get; init; }

    public required Distribution Distribution { get; init; }

    public string? Notes { get; init; }

    public bool HasValidGameExecutable => !string.IsNullOrWhiteSpace(GameExecutablePath);

    public bool HasValidLauncherExecutable => !string.IsNullOrWhiteSpace(LauncherExecutablePath);

    public bool IsValid => HasValidGameExecutable || HasValidLauncherExecutable;
}
