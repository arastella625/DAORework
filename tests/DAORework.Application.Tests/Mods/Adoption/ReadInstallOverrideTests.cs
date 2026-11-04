using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using DAORework.Application.Mods.Adoption;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Adoption;
using Xunit;

namespace DAORework.Application.Tests.Mods.Adoption;

/// <summary>Reading the install folder's override (P2.5-22).</summary>
public sealed class ReadInstallOverrideTests
{
    private const string Install = "C:/Game";

    private readonly MockFileSystem _fileSystem = new();

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public void Execute_NoInstallFolder_IsEmpty(string? install)
    {
        Assert.Same(InstallOverrideContents.Empty, new ReadInstallOverride(_fileSystem).Execute(install).Value);
    }

    [Fact]
    public void Execute_NoOverrideFolder_IsEmpty()
    {
        _fileSystem.AddDirectory(Install);

        Assert.Same(InstallOverrideContents.Empty, new ReadInstallOverride(_fileSystem).Execute(Install).Value);
    }

    [Fact]
    public void Execute_ListsEachFolderAndTheLooseFiles_WithForwardSlashes()
    {
        var folder = GamePaths.OverrideDirectory(Install);
        _fileSystem.AddFile(Path.Combine(folder, "Old Hair", "deep", "hair.dds"), new MockFileData(new byte[10]));
        _fileSystem.AddFile(Path.Combine(folder, "Old Hair", "hair2.dds"), new MockFileData(new byte[5]));
        _fileSystem.AddFile(Path.Combine(folder, "fix.ncs"), new MockFileData(new byte[1]));

        var contents = new ReadInstallOverride(_fileSystem).Execute(Install).Value;

        Assert.Contains("Old Hair/deep/hair.dds", contents.Paths);
        Assert.Equal(
            [new InstallOverrideMod("Old Hair", 2, 15), new InstallOverrideMod(InstallOverrideContents.LooseFilesName, 1, 1)],
            contents.Mods);
    }

    [Fact]
    public void Execute_AFolderThatCannotBeRead_FailsSayingSo()
    {
        var fileSystem = new DeniedFileSystem();
        fileSystem.AddDirectory(GamePaths.OverrideDirectory(Install));
        fileSystem.Denied = true;

        var result = new ReadInstallOverride(fileSystem).Execute(Install);

        Assert.Equal(DiagnosticCodes.InstallOverrideUnreadable, result.Error!.Code);
    }

    private sealed class DeniedFileSystem : MockFileSystem
    {
        public bool Denied { get; set; }

        public override IDirectoryInfoFactory DirectoryInfo => Denied ? throw new UnauthorizedAccessException("denied") : base.DirectoryInfo;
    }
}
