using System.IO.Abstractions.TestingHelpers;
using DAORework.Core.Diagnostics;
using DAORework.Infrastructure.Mods;
using Xunit;

namespace DAORework.Infrastructure.Tests.Mods;

public sealed class Sha256FileHasherTests
{
    [Fact]
    public async Task ComputeSha256Async_KnownContent_ReturnsUpperHexDigest()
    {
        var path = MockUnixSupport.Path(@"C:\mods\abc.txt");
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile(path, new MockFileData("abc"));

        var result = await new Sha256FileHasher(fileSystem).ComputeSha256Async(path);

        Assert.True(result.IsSuccess);
        Assert.Equal("BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD", result.Value);
    }

    [Fact]
    public async Task ComputeSha256Async_MissingFile_ReturnsReadFailed()
    {
        var result = await new Sha256FileHasher(new MockFileSystem()).ComputeSha256Async(MockUnixSupport.Path(@"C:\mods\missing.txt"));

        Assert.True(result.IsFailure);
        Assert.Equal(DiagnosticCodes.FileReadFailed, result.Error!.Code);
    }
}
