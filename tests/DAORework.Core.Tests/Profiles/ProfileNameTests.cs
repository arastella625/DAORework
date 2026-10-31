using DAORework.Core.Diagnostics;
using DAORework.Core.Profiles;
using Xunit;

namespace DAORework.Core.Tests.Profiles;

public sealed class ProfileNameTests
{
    [Theory]
    [InlineData("Default")]
    [InlineData("My Profile")]
    [InlineData("mods-v2_final")]
    [InlineData("Ünïcödé")]
    [InlineData("v1.2")]
    public void Create_ValidName_Succeeds(string candidate)
    {
        var result = ProfileName.Create(candidate);

        Assert.True(result.IsSuccess);
        Assert.Equal(candidate, result.Value.Value);
    }

    [Fact]
    public void Create_TrimsSurroundingWhitespace()
    {
        var result = ProfileName.Create("  Default \t");

        Assert.Equal("Default", result.Value.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Empty_FailsWithEmptyCode(string? candidate)
    {
        var result = ProfileName.Create(candidate);

        Assert.True(result.IsFailure);
        Assert.Equal(DiagnosticCodes.ProfileNameEmpty, result.Error!.Code);
    }

    [Fact]
    public void Create_TooLong_FailsWithTooLongCode()
    {
        var result = ProfileName.Create(new string('a', ProfileName.MaxLength + 1));

        Assert.Equal(DiagnosticCodes.ProfileNameTooLong, result.Error!.Code);
    }

    [Fact]
    public void Create_ExactlyMaxLength_Succeeds()
    {
        Assert.True(ProfileName.Create(new string('a', ProfileName.MaxLength)).IsSuccess);
    }

    [Theory]
    [InlineData("My:Profile")]
    [InlineData("a/b")]
    [InlineData("a\\b")]
    [InlineData("what?")]
    [InlineData("star*")]
    [InlineData("<tag>")]
    [InlineData("pipe|")]
    [InlineData("quote\"")]
    [InlineData("tab\tname")]
    [InlineData("trailing.")]
    [InlineData("..")]
    public void Create_InvalidCharacters_FailsWithInvalidCharactersCode(string candidate)
    {
        var result = ProfileName.Create(candidate);

        Assert.Equal(DiagnosticCodes.ProfileNameInvalidCharacters, result.Error!.Code);
        Assert.NotNull(result.Error.Recommendation);
    }

    [Theory]
    [InlineData("CON")]
    [InlineData("con")]
    [InlineData("NUL.json")]
    [InlineData("COM1")]
    [InlineData("lpt9")]
    public void Create_ReservedWindowsDeviceName_FailsWithReservedCode(string candidate)
    {
        var result = ProfileName.Create(candidate);

        Assert.Equal(DiagnosticCodes.ProfileNameReserved, result.Error!.Code);
    }

    [Fact]
    public void Equality_IsByValue()
    {
        Assert.Equal(ProfileName.Create("A").Value, ProfileName.Create("A").Value);
        Assert.NotEqual(ProfileName.Create("A").Value, ProfileName.Create("a").Value);
        Assert.Equal("Default", ProfileName.Default.ToString());
    }
}
