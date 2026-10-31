using DAORework.Core.Support;
using Xunit;

namespace DAORework.Core.Tests.Support;

public sealed class PathRedactorTests
{
    private readonly PathRedactor _redactor = PathRedactor.ForUserProfile(@"C:\Users\player");

    [Theory]
    [InlineData(@"C:\Users\player\Documents\BioWare", @"C:\Users\<user>\Documents\BioWare")]
    [InlineData(@"""path"": ""C:\\Users\\player\\Documents""", @"""path"": ""C:\\Users\\<user>\\Documents""")]
    [InlineData("/home/player/.local/share", "/home/<user>/.local/share")]
    [InlineData(@"C:\users\Someone Else\file.txt", @"C:\users\<user>\file.txt")]
    [InlineData(@"D:\SteamLibrary\steamapps\common\Dragon Age", @"D:\SteamLibrary\steamapps\common\Dragon Age")]
    public void Redact_ReplacesTheFolderAfterUsersOrHome(string input, string expected)
    {
        Assert.Equal(expected, _redactor.Redact(input));
    }

    [Fact]
    public void Redact_ReplacesTheBareAccountNameAsAWholeWord()
    {
        Assert.Equal("Signed in as <user> on GAMING-PC.", _redactor.Redact("Signed in as player on GAMING-PC."));
    }

    [Fact]
    public void Redact_ShortAccountNames_AreOnlyRedactedInPaths()
    {
        var redactor = PathRedactor.ForUserProfile(@"C:\Users\Me");

        Assert.Equal(@"Me at C:\Users\<user>\x", redactor.Redact(@"Me at C:\Users\Me\x"));
    }

    [Fact]
    public void Redact_IsIdempotent()
    {
        var once = _redactor.Redact(@"C:\Users\player\a");

        Assert.Equal(once, _redactor.Redact(once));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ForUserProfile_Unknown_StillRedactsPaths(string? userProfile)
    {
        Assert.Equal(@"C:\Users\<user>\x", PathRedactor.ForUserProfile(userProfile).Redact(@"C:\Users\anyone\x"));
    }
}
