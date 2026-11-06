using System.IO.Abstractions.TestingHelpers;
using System.IO.Compression;
using System.Text;
using DAORework.Core.Diagnostics;
using DAORework.Infrastructure.KnownFolders;
using DAORework.Infrastructure.Mods;
using DAORework.Infrastructure.Tests.Mods.Archives;
using Xunit;

namespace DAORework.Infrastructure.Tests.Mods;

/// <summary>Reading a downloads folder (ADR-0034 §2, §4): top level, finished files, names only, and nothing written.</summary>
public sealed class FolderDownloadsTests
{
    private static readonly string _folder = MockUnixSupport.Path(@"E:\Get");

    private readonly MockFileSystem _fileSystem = new();

    private FolderDownloads Downloads => new(_fileSystem);

    [Fact]
    public void ListFinished_AMissingFolder_SaysSo()
    {
        Assert.Equal(DiagnosticCodes.DownloadsFolderMissing, Downloads.ListFinished(_folder).Error!.Code);
    }

    [Fact]
    public void ListFinished_TakesFinishedDownloadsAtTheTopLevelOnly()
    {
        Add("mod.zip", "x");
        Add("other.7z", "x");
        Add("empty.zip", string.Empty);
        Add("half.zip", "x");
        Add("half.zip.part", "x");
        Add("going.crdownload", "x");
        Add("setup.exe", "x");
        Add(Path.Combine("sub", "deep.zip"), "x");

        var names = Downloads.ListFinished(_folder).Value.Select(f => f.FileName).Order();

        Assert.Equal(["mod.zip", "other.7z"], names);
    }

    [Fact]
    public void ReadEntryNames_AnExtractedFolder_GivesItsFilesWithForwardSlashes()
    {
        Add(Path.Combine("Blue Hair", "Hair", "a.erf"), "x");
        Add(Path.Combine("Blue Hair", "readme.txt"), "x");

        Assert.Equal(["Hair/a.erf", "readme.txt"], Downloads.ReadEntryNames(Path.Combine(_folder, "Blue Hair")).Value.Order());
    }

    [Fact]
    public void ReadEntryNames_ADazip_IsReadAsTheZipItIs()
    {
        _fileSystem.AddFile(Path.Combine(_folder, "mod.dazip"), new MockFileData(Zip("Manifest.xml", "Contents/addins/uid/a.erf")));

        Assert.Equal(["Manifest.xml", "Contents/addins/uid/a.erf"], Downloads.ReadEntryNames(Path.Combine(_folder, "mod.dazip")).Value);
    }

    [Fact]
    public void ReadEntryNames_AZip_GivesItsFileNamesWithForwardSlashes()
    {
        _fileSystem.AddFile(Path.Combine(_folder, "mod.zip"), new MockFileData(Zip("Hair/a.erf", "readme.txt")));

        Assert.Equal(["Hair/a.erf", "readme.txt"], Downloads.ReadEntryNames(Path.Combine(_folder, "mod.zip")).Value);
    }

    [Fact]
    public void ReadEntryNames_ARealRarAnd7z_GiveTheirFileNames()
    {
        _fileSystem.AddFile(Path.Combine(_folder, "a.rar"), new MockFileData(TestArchives.Rar(("Hair/a.erf", "x"))));
        _fileSystem.AddFile(Path.Combine(_folder, "b.7z"), new MockFileData(TestArchives.SevenZip(("Hair/b.erf", "x"))));

        Assert.Equal(["Hair/a.erf"], Downloads.ReadEntryNames(Path.Combine(_folder, "a.rar")).Value);
        Assert.Equal(["Hair/b.erf"], Downloads.ReadEntryNames(Path.Combine(_folder, "b.7z")).Value);
    }

    [Fact]
    public void KnownFolders_DownloadsIsWhatTheShellReports_OnWindows()
    {
        var downloads = new EnvironmentKnownFolders().Downloads;

        Assert.Equal(OperatingSystem.IsWindows(), Path.IsPathRooted(downloads));
    }

    [Theory]
    [InlineData("bad.zip")]
    [InlineData("bad.7z")]
    [InlineData("bad.rar")]
    public void ReadEntryNames_NotReallyAnArchive_FailsWithoutThrowing(string name)
    {
        _fileSystem.AddFile(Path.Combine(_folder, name), new MockFileData(Encoding.ASCII.GetBytes("this is no archive at all, just text")));

        Assert.Equal(DiagnosticCodes.DownloadsUnreadable, Downloads.ReadEntryNames(Path.Combine(_folder, name)).Error!.Code);
    }

    [Fact]
    public void ReadEntryNames_ATruncatedZip_FailsWithoutThrowing()
    {
        var bytes = Zip("a.erf", "b.erf");
        _fileSystem.AddFile(Path.Combine(_folder, "cut.zip"), new MockFileData(bytes[..(bytes.Length / 2)]));

        Assert.True(Downloads.ReadEntryNames(Path.Combine(_folder, "cut.zip")).IsFailure);
    }

    [Fact]
    public void NothingIsEverWritten()
    {
        _fileSystem.AddFile(Path.Combine(_folder, "mod.zip"), new MockFileData(Zip("a.erf")));
        var before = _fileSystem.AllFiles.ToList();

        Downloads.ListFinished(_folder);
        Downloads.ReadEntryNames(Path.Combine(_folder, "mod.zip"));

        Assert.Equal(before, _fileSystem.AllFiles);
    }

    private static byte[] Zip(params string[] entries)
    {
        using var buffer = new MemoryStream();
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var entry in entries)
            {
                using var writer = new StreamWriter(zip.CreateEntry(entry).Open());
                writer.Write("x");
            }
        }

        return buffer.ToArray();
    }

    private void Add(string relative, string content) => _fileSystem.AddFile(Path.Combine(_folder, relative), new MockFileData(content));
}
