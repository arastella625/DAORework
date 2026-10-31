using DAORework.Core.Formats;
using Xunit;

namespace DAORework.Core.Tests.Formats;

public sealed class IniDocumentTests
{
    private const string Settings = "; written by the game\r\n[Graphics]\r\nWidth=1920\r\nHeight = 1080 \r\n\r\n[Sound]\r\nVolume=80\r\n# comment\r\nnot a pair\r\n";

    [Fact]
    public void Parse_ReadsSectionsAndTrimmedValues()
    {
        var document = IniDocument.Parse(Settings);

        Assert.Equal(["Graphics", "Sound"], document.Sections.Order());
        Assert.Equal("1920", document.Get("Graphics", "Width"));
        Assert.Equal("1080", document.Get("graphics", "HEIGHT"));
        Assert.Null(document.Get("Graphics", "Depth"));
        Assert.Null(document.Get("Missing", "Width"));
    }

    [Fact]
    public void FindAnywhere_ReturnsTheKeyFromWhicheverSectionHasIt()
    {
        var document = IniDocument.Parse(Settings);

        Assert.Equal("80", document.FindAnywhere("volume"));
        Assert.Null(document.FindAnywhere("Nope"));
    }

    [Fact]
    public void Parse_KeysBeforeAnySection_LandInTheUnnamedSection()
    {
        var document = IniDocument.Parse("Key=Value");

        Assert.Equal("Value", document.Get(string.Empty, "Key"));
    }
}
