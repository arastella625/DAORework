using System.IO.Abstractions.TestingHelpers;
using DAORework.Core.Diagnostics;
using DAORework.Infrastructure.Fixes;
using Xunit;

namespace DAORework.Infrastructure.Tests.Fixes;

public sealed class InstallFolderFilesTests
{
    private const string Exe = "bin_ship/daorigins.exe";

    private static readonly string _install = MockUnixSupport.Path(@"D:\SteamLibrary\steamapps\common\Dragon Age Ultimate Edition");

    private readonly MockFileSystem _fileSystem = new();
    private readonly InstallFolderFiles _files;

    public InstallFolderFilesTests()
    {
        _fileSystem.AddFile(Full(Exe), new MockFileData("exe"));
        _files = new InstallFolderFiles(_fileSystem);
    }

    [Fact]
    public async Task Read_ReturnsTheBytesOrSaysTheFileIsMissing()
    {
        Assert.Equal("exe"u8.ToArray(), (await _files.ReadAsync(_install, Exe)).Value);
        Assert.Equal(DiagnosticCodes.FixInstallUnreadable, (await _files.ReadAsync(_install, "bin_ship/nope.exe")).Error!.Code);
    }

    [Fact]
    public async Task Hash_LeavesOutPathsWithNoFile()
    {
        var hashes = (await _files.HashAsync(_install, [Exe, "bin_ship/d3d9.dll"])).Value;

        Assert.Equal([Exe], hashes.Keys);
        Assert.Equal(64, hashes[Exe].Length);
    }

    [Fact]
    public async Task Hash_LockedFile_Fails()
    {
        _fileSystem.AddFile(Full("bin_ship/locked.dll"), new MockFileData("x") { AllowedFileShare = FileShare.None });

        Assert.Equal(DiagnosticCodes.FixInstallUnreadable, (await _files.HashAsync(_install, ["bin_ship/locked.dll"])).Error!.Code);
    }

    [Fact]
    public async Task CheckWritable_FolderWithNoFileInIt_IsNotCheckedHere()
    {
        _fileSystem.AddDirectory(Full("empty"));

        Assert.Empty(await _files.CheckWritableAsync(_install, ["empty/d3d9.dll"]));
    }

    [Fact]
    public async Task CheckWritable_WritableFolderAndFile_IsFineAndLeavesNothingBehind()
    {
        Assert.Empty(await _files.CheckWritableAsync(_install, [Exe, "bin_ship/d3d9.dll"]));
        Assert.Equal([Full(Exe)], _fileSystem.Directory.GetFiles(Full("bin_ship")));
    }

    [Fact]
    public async Task CheckWritable_FolderWithoutWriteAccess_IsNotWritableEvenForAFileNotThereYet()
    {
        _fileSystem.AddFile(Full(Exe), new MockFileData("exe") { Attributes = FileAttributes.ReadOnly });

        var problem = Assert.Single(await _files.CheckWritableAsync(_install, ["bin_ship/d3d9.dll"]));

        Assert.Equal(DiagnosticCodes.FixInstallNotWritable, problem.Code);
        Assert.Contains("administrator", problem.Recommendation, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CheckWritable_FileTheGameHoldsOpen_IsInUse()
    {
        _fileSystem.AddFile(Full(Exe), new MockFileData("exe") { AllowedFileShare = FileShare.None });

        Assert.Equal(DiagnosticCodes.FixFileInUse, Assert.Single(await _files.CheckWritableAsync(_install, [Exe])).Code);
    }

    [Fact]
    public async Task CheckWritable_ReadOnlyFile_IsNotWritable()
    {
        _fileSystem.AddFile(Full(Exe), new MockFileData("exe") { Attributes = FileAttributes.ReadOnly });

        Assert.Equal(DiagnosticCodes.FixInstallNotWritable, Assert.Single(await _files.CheckWritableAsync(_install, [Exe])).Code);
    }

    [Fact]
    public async Task PathOutsideTheInstall_IsRefused()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => _files.ReadAsync(_install, "../evil.exe"));
    }

    private static string Full(string relative) => Path.Combine(_install, relative.Replace('/', Path.DirectorySeparatorChar));
}
