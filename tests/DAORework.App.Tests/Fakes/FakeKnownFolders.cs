using System.IO.Abstractions.TestingHelpers;
using DAORework.Application.Abstractions;

namespace DAORework.App.Tests.Fakes;

internal sealed class FakeKnownFolders : IKnownFolders
{
    public string MyDocuments { get; init; } = MockUnixSupport.Path(@"C:\Users\Me\Documents");

    public string UserProfile { get; init; } = MockUnixSupport.Path(@"C:\Users\Me");

    public string? OneDrive { get; init; }

    public string LocalApplicationData { get; init; } = MockUnixSupport.Path(@"C:\Users\Me\AppData\Local");

    public string ProgramFiles { get; init; } = MockUnixSupport.Path(@"C:\Program Files");

    public string ProgramFilesX86 { get; init; } = MockUnixSupport.Path(@"C:\Program Files (x86)");

    public string Windows { get; init; } = MockUnixSupport.Path(@"C:\Windows");

    public string SystemX86 { get; init; } = MockUnixSupport.Path(@"C:\Windows\SysWOW64");
}
