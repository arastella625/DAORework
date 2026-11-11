using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using DAORework.Application.Documents;
using DAORework.Application.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DAORework.Application.Tests.Documents;

/// <summary>#205: the one comparison both documents rules use (ADR-0046).</summary>
public sealed class SameFolderTests
{
    private static readonly string _folder = TestPaths.Root("BioWare", "Dragon Age");

    private readonly MockFileSystem _fileSystem = new();

    [Fact]
    public void AreSame_DifferentCaseAndTrailingSeparator_IsTheSameFolder()
    {
        Assert.True(Same().AreSame(_folder, _folder.ToUpperInvariant() + Path.DirectorySeparatorChar));
    }

    [Fact]
    public void AreSame_TwoFolders_IsNot()
    {
        _fileSystem.AddDirectory(_folder);
        _fileSystem.AddDirectory(TestPaths.Root("Other", "Dragon Age"));

        Assert.False(Same().AreSame(_folder, TestPaths.Root("Other", "Dragon Age")));
    }

    [Fact]
    public void AreSame_ALinkAndTheFolderItPointsAt_IsTheSameFolder()
    {
        var link = TestPaths.Root("Linked");
        _fileSystem.AddDirectory(_folder);
        _fileSystem.Directory.CreateSymbolicLink(link, _folder);

        Assert.True(Same().AreSame(link, _folder));
        Assert.True(Same().AreSame(_folder, link));
    }

    [Fact]
    public void AreSame_AFolderUnderALink_IsTheSameFolder()
    {
        var linked = TestPaths.Root("Linked");
        _fileSystem.AddDirectory(_folder);
        _fileSystem.Directory.CreateSymbolicLink(linked, Path.GetDirectoryName(_folder)!);

        Assert.True(Same().AreSame(Path.Combine(linked, "Dragon Age"), _folder));
    }

    [Fact]
    public void AreSame_ARelativePath_IsComparedAsWritten()
    {
        var relative = Path.Combine("BioWare", "Dragon Age");
        _fileSystem.Directory.SetCurrentDirectory(TestPaths.Root());
        _fileSystem.AddDirectory(_folder);

        Assert.False(Same().AreSame(relative, _folder));
        Assert.True(Same().AreSame(relative, relative.ToUpperInvariant()));
    }

    [Fact]
    public void AreSame_LinksThatCannotBeRead_ComparesAsWritten()
    {
        var denied = new DeniedFileSystem();
        denied.AddDirectory(_folder);
        var link = TestPaths.Root("Linked");
        denied.Directory.CreateSymbolicLink(link, _folder);
        denied.Denied = true;

        var same = new SameFolder(denied, NullLogger<SameFolder>.Instance);

        Assert.False(same.AreSame(link, _folder));
        Assert.True(same.AreSame(_folder, _folder + Path.DirectorySeparatorChar));
    }

    private SameFolder Same() => new(_fileSystem, NullLogger<SameFolder>.Instance);

    private sealed class DeniedFileSystem : MockFileSystem
    {
        public bool Denied { get; set; }

        public override IDirectoryInfoFactory DirectoryInfo => Denied ? throw new UnauthorizedAccessException("denied") : base.DirectoryInfo;
    }
}
