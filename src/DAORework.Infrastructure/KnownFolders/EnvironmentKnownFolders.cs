using DAORework.Application.Abstractions;

namespace DAORework.Infrastructure.KnownFolders;

public sealed class EnvironmentKnownFolders : IKnownFolders
{
    public string MyDocuments => Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

    public string UserProfile => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    public string? OneDrive => Environment.GetEnvironmentVariable("OneDrive") is { Length: > 0 } root ? root : null;

    public string LocalApplicationData => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

    public string ProgramFiles => Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

    public string ProgramFilesX86 => Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

    public string Windows => Environment.GetFolderPath(Environment.SpecialFolder.Windows);

    public string SystemX86 => Environment.GetFolderPath(Environment.SpecialFolder.SystemX86);
}
