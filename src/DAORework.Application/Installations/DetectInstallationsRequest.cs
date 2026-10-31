namespace DAORework.Application.Installations;

/// <param name="ManualPaths">Folders the user named, probed as <see cref="Core.Installations.Distribution.Manual"/> installs alongside the locators.</param>
public sealed record DetectInstallationsRequest(IReadOnlyList<string>? ManualPaths = null);
