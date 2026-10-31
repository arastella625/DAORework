using DAORework.Core.Diagnostics;
using DAORework.Core.Looks;
using Xunit;

namespace DAORework.Core.Tests.Looks;

public sealed class LookNamesTests
{
    [Theory]
    [InlineData("Wilds run", "Wilds run")]
    [InlineData("  Morrigan  ", "Morrigan")]
    public void ValidName_TrimsAName(string name, string expected)
    {
        Assert.Equal(expected, LookNames.ValidName(name).Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("A name that is far too long to fit on a theme card")]
    public void ValidName_RefusesEmptyAndLongNames(string? name)
    {
        Assert.Equal(DiagnosticCodes.LookNameInvalid, LookNames.ValidName(name).Error!.Code);
    }

    [Theory]
    [InlineData("look-1a2b3c4d5e6f", true)]
    [InlineData("witch-of-the-wilds", false)]
    [InlineData("look-../../Windows", false)]
    [InlineData("look-ABC", false)]
    [InlineData("look-", false)]
    [InlineData(null, false)]
    public void IsValidId_AcceptsOnlyTheStrictForm(string? id, bool valid)
    {
        Assert.Equal(valid, LookNames.IsValidId(id));
    }

    [Fact]
    public void NewId_IsAValidIdAndNotABuiltInTheme()
    {
        var id = LookNames.NewId();

        Assert.True(LookNames.IsValidId(id));
        Assert.StartsWith(Look.IdPrefix, id, StringComparison.Ordinal);
        Assert.NotEqual(id, LookNames.NewId());
    }
}
