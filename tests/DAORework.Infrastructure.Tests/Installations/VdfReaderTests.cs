using DAORework.Infrastructure.Installations.Steam;
using Xunit;

namespace DAORework.Infrastructure.Tests.Installations;

public sealed class VdfReaderTests
{
    private const string LibraryFolders = """
        "libraryfolders"
        {
            "0"
            {
                "path"        "C:\\Program Files (x86)\\Steam"
                "label"        ""
                "apps"
                {
                    "228980"        "183916578"
                }
            }
            "1"
            {
                "path"        "D:\\SteamLibrary"   // the second drive
                "apps"
                {
                    "47810"        "12345"
                }
            }
        }
        """;

    [Fact]
    public void Parse_LibraryFolders_ExposesEveryLibraryPathWithEscapesResolved()
    {
        var root = VdfReader.Parse(LibraryFolders);

        var folders = root.GetChild("libraryfolders");
        Assert.NotNull(folders);
        Assert.Equal(2, folders.Children.Count);
        Assert.Equal(@"C:\Program Files (x86)\Steam", folders.GetChild("0")!.GetString("path"));
        Assert.Equal(@"D:\SteamLibrary", folders.GetChild("1")!.GetString("path"));
        Assert.Equal("12345", folders.GetChild("1")!.GetChild("apps")!.GetString("47810"));
        Assert.Equal(string.Empty, folders.GetChild("0")!.GetString("label"));
    }

    [Fact]
    public void Parse_AppManifest_ReadsTheInstallDir()
    {
        const string manifest = """
            "AppState"
            {
                "appid"        "47810"
                "name"        "Dragon Age: Origins - Ultimate Edition"
                "installdir"        "Dragon Age Ultimate Edition"
            }
            """;

        var root = VdfReader.Parse(manifest);

        Assert.Equal("Dragon Age Ultimate Edition", root.GetChild("AppState")!.GetString("installdir"));
        Assert.Null(root.GetChild("AppState")!.GetString("missing"));
    }

    [Fact]
    public void Parse_KeysAreCaseInsensitive()
    {
        var root = VdfReader.Parse("\"Root\" { \"Key\" \"value\" }");

        Assert.Equal("value", root.GetChild("root")!.GetString("KEY"));
    }

    [Theory]
    [InlineData("\"a\" { \"b\" \"c\"")]
    [InlineData("\"a\" \"b\" }")]
    [InlineData("\"a\"")]
    [InlineData("\"unterminated")]
    public void Parse_MalformedText_ThrowsInvalidData(string text)
    {
        Assert.Throws<InvalidDataException>(() => VdfReader.Parse(text));
    }
}
