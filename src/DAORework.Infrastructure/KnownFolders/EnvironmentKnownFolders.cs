using DAORework.Application.Abstractions;

namespace DAORework.Infrastructure.KnownFolders;

/// <summary>
/// The folders the platform reports. A folder that doesn't exist yet keeps its name (ADR-0031): on Linux .NET otherwise
/// returns an empty string for one, which joined into a path would quietly mean the folder DAORework was started from.
/// The Windows-only folders stay empty off Windows, and whatever reads them skips an empty one.
/// </summary>
public sealed class EnvironmentKnownFolders : IKnownFolders
{
    public string MyDocuments => Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments, Environment.SpecialFolderOption.DoNotVerify);

    public string Downloads => ShellDownloadsFolder.Path();

    public string UserProfile => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile, Environment.SpecialFolderOption.DoNotVerify);

    public string? OneDrive => Environment.GetEnvironmentVariable("OneDrive") is { Length: > 0 } root ? root : null;

    public string LocalApplicationData => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.DoNotVerify);

    public string ProgramFiles => Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

    public string ProgramFilesX86 => Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

    public string Windows => Environment.GetFolderPath(Environment.SpecialFolder.Windows);

    public string SystemX86 => Environment.GetFolderPath(Environment.SpecialFolder.SystemX86);
}
