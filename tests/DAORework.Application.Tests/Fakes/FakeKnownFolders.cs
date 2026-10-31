using DAORework.Application.Abstractions;

namespace DAORework.Application.Tests.Fakes;

internal sealed class FakeKnownFolders : IKnownFolders
{
    public string MyDocuments { get; init; } = TestPaths.Root("Users", "Me", "Documents");

    public string UserProfile { get; init; } = TestPaths.Root("Users", "Me");

    public string? OneDrive { get; init; }

    public string LocalApplicationData { get; init; } = TestPaths.Root("Users", "Me", "AppData", "Local");

    public string ProgramFiles { get; init; } = TestPaths.Root("Program Files");

    public string ProgramFilesX86 { get; init; } = TestPaths.Root("Program Files (x86)");

    public string Windows { get; init; } = TestPaths.Root("Windows");

    public string SystemX86 { get; init; } = TestPaths.Root("Windows", "SysWOW64");
}
