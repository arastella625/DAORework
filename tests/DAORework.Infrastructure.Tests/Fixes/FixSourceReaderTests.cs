using System.Formats.Tar;
using System.IO.Abstractions.TestingHelpers;
using System.IO.Compression;
using DAORework.Core.Diagnostics;
using DAORework.Infrastructure.Fixes;
using Xunit;

namespace DAORework.Infrastructure.Tests.Fixes;

public sealed class FixSourceReaderTests
{
    private static readonly string _downloads = MockUnixSupport.Path(@"C:\Downloads");
    private static readonly byte[] _x32 = "x32 dll"u8.ToArray();
    private static readonly byte[] _x64 = "x64 dll"u8.ToArray();

    private readonly MockFileSystem _fileSystem = new();
    private readonly FixSourceReader _reader;

    public FixSourceReaderTests()
    {
        _reader = new FixSourceReader(_fileSystem);
    }

    [Theory]
    [InlineData("dxvk-2.3.tar.gz")]
    [InlineData("dxvk-2.3.tgz")]
    public async Task TarGz_AsDxvkPublishesIt_GivesTheX32Dll(string name)
    {
        var path = Add(name, TarGz(("dxvk-2.3/x64/d3d9.dll", _x64), ("dxvk-2.3/x32/d3d9.dll", _x32)));

        Assert.Equal(_x32, (await _reader.ReadDxvkDllAsync(path)).Value);
    }

    [Fact]
    public async Task Zip_GivesTheX32Dll()
    {
        var path = Add("dxvk.zip", Zip(("dxvk-2.3/x64/d3d9.dll", _x64), ("dxvk-2.3/x32/d3d9.dll", _x32)));

        Assert.Equal(_x32, (await _reader.ReadDxvkDllAsync(path)).Value);
    }

    [Fact]
    public async Task ExtractedFolder_GivesTheX32Dll()
    {
        var folder = Path.Combine(_downloads, "dxvk-2.3");
        _fileSystem.AddFile(Path.Combine(folder, "x64", "d3d9.dll"), new MockFileData(_x64));
        _fileSystem.AddFile(Path.Combine(folder, "x32", "d3d9.dll"), new MockFileData(_x32));

        Assert.Equal(_x32, (await _reader.ReadDxvkDllAsync(folder)).Value);
    }

    [Fact]
    public async Task TheDllItself_IsReadAsItIs()
    {
        Assert.Equal(_x32, (await _reader.ReadDxvkDllAsync(Add("d3d9.dll", _x32))).Value);
    }

    [Fact]
    public async Task ArchiveWithoutX32_SaysSo()
    {
        var result = await _reader.ReadDxvkDllAsync(Add("other.zip", Zip(("readme.txt", "hi"u8.ToArray()))));

        Assert.Contains("has no x32/d3d9.dll", result.Error!.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task FolderWithoutX32_SaysSo()
    {
        _fileSystem.AddFile(Path.Combine(_downloads, "empty", "x64", "d3d9.dll"), new MockFileData(_x64));

        Assert.Equal(DiagnosticCodes.FixSourceInvalid, (await _reader.ReadDxvkDllAsync(Path.Combine(_downloads, "empty"))).Error!.Code);
    }

    [Theory]
    [InlineData("dxvk.7z")]
    [InlineData("missing.tar.gz")]
    public async Task SomethingItCannotRead_IsInvalid(string name)
    {
        if (name.EndsWith(".7z", StringComparison.Ordinal))
        {
            Add(name, [0x37, 0x7A]);
        }

        Assert.Equal(DiagnosticCodes.FixSourceInvalid, (await _reader.ReadDxvkDllAsync(Path.Combine(_downloads, name))).Error!.Code);
    }

    [Fact]
    public async Task CorruptArchive_IsInvalidInsteadOfThrowing()
    {
        Assert.Equal(DiagnosticCodes.FixSourceInvalid, (await _reader.ReadDxvkDllAsync(Add("broken.tar.gz", "not gzip"u8.ToArray()))).Error!.Code);
    }

    private static byte[] TarGz(params (string Name, byte[] Content)[] entries)
    {
        using var buffer = new MemoryStream();
        using (var gzip = new GZipStream(buffer, CompressionMode.Compress, leaveOpen: true))
        using (var tar = new TarWriter(gzip))
        {
            foreach (var (name, content) in entries)
            {
                tar.WriteEntry(new PaxTarEntry(TarEntryType.RegularFile, name) { DataStream = new MemoryStream(content) });
            }
        }

        return buffer.ToArray();
    }

    private static byte[] Zip(params (string Name, byte[] Content)[] entries)
    {
        using var buffer = new MemoryStream();
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (name, content) in entries)
            {
                using var stream = zip.CreateEntry(name).Open();
                stream.Write(content);
            }
        }

        return buffer.ToArray();
    }

    private string Add(string name, byte[] content)
    {
        var path = Path.Combine(_downloads, name);
        _fileSystem.AddFile(path, new MockFileData(content));
        return path;
    }
}
