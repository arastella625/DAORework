using System.IO.Abstractions.TestingHelpers;
using DAORework.Core.Common;
using DAORework.Infrastructure.KnownFolders;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DAORework.Infrastructure.Tests.KnownFolders;

public sealed class DriveDocumentsFolderSearchTests
{
    private const string Drive = "C:";

    private readonly MockFileSystem _fileSystem = new();
    private readonly string _root;

    public DriveDocumentsFolderSearchTests()
    {
        _fileSystem.AddDrive(Drive, new MockDriveData { DriveType = DriveType.Fixed });

        // Read back, not assumed: the mock's root for a drive differs by OS (C:\ on Windows, /C: elsewhere).
        _root = _fileSystem.DriveInfo.GetDrives().Single(d => d.DriveType == DriveType.Fixed).RootDirectory.FullName;
    }

    private DriveDocumentsFolderSearch Search => new(_fileSystem, NullLogger<DriveDocumentsFolderSearch>.Instance);

    [Fact]
    public async Task FindAsync_NothingThere_FindsNothing()
    {
        Assert.Empty(await Search.FindAsync());
    }

    [Theory]
    [InlineData(DriveType.Removable, true)]
    [InlineData(DriveType.Network, true)]
    [InlineData(DriveType.CDRom, true)]
    [InlineData(DriveType.Fixed, false)]
    public async Task FindAsync_DriveThatIsNotFixedOrNotReady_IsNotSearched(DriveType type, bool isReady)
    {
        _fileSystem.AddDrive(Drive, new MockDriveData { DriveType = type, IsReady = isReady });
        _fileSystem.AddDirectory(DocumentsUnder());

        Assert.Empty(await Search.FindAsync());
    }

    [Fact]
    public async Task FindAsync_FolderAtTheDrivesRoot_FindsIt()
    {
        var folder = DocumentsUnder();
        _fileSystem.AddDirectory(folder);

        Assert.Equal(folder, Assert.Single(await Search.FindAsync()));
    }

    [Fact]
    public async Task FindAsync_FolderInADocumentsFolderThreeDeep_FindsIt()
    {
        var folder = DocumentsUnder("Users", "Me", "Documents");
        _fileSystem.AddDirectory(folder);

        Assert.Equal(folder, Assert.Single(await Search.FindAsync()));
    }

    [Fact]
    public async Task FindAsync_FolderFourDeep_IsTooFarDown()
    {
        _fileSystem.AddDirectory(DocumentsUnder("One", "Two", "Three", "Four"));

        Assert.Empty(await Search.FindAsync());
    }

    [Theory]
    [InlineData("Windows")]
    [InlineData("program files")]
    [InlineData("Program Files (x86)")]
    [InlineData("ProgramData")]
    [InlineData("$Recycle.Bin")]
    [InlineData("System Volume Information")]
    public async Task FindAsync_FolderInsideASystemFolder_IsSkipped(string systemFolder)
    {
        _fileSystem.AddDirectory(DocumentsUnder(systemFolder, "Old"));

        Assert.Empty(await Search.FindAsync());
    }

    [Fact]
    public async Task FindAsync_SystemFolderNameBelowTheRoot_IsNotSkipped()
    {
        var folder = DocumentsUnder("Backup", "Windows");
        _fileSystem.AddDirectory(folder);

        Assert.Equal(folder, Assert.Single(await Search.FindAsync()));
    }

    [Fact]
    public async Task FindAsync_SeveralFolders_ReturnsThemInPathOrder()
    {
        var second = DocumentsUnder("Zebra");
        var first = DocumentsUnder("Alpha");
        _fileSystem.AddDirectory(second);
        _fileSystem.AddDirectory(first);

        Assert.Equal([first, second], await Search.FindAsync());
    }

    [Fact]
    public async Task FindAsync_Cancelled_Stops()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Search.FindAsync(cancellation.Token));
    }

    private string DocumentsUnder(params string[] folders) =>
        GamePaths.DefaultDocumentsDirectory(Path.Combine([_root, .. folders]));
}
