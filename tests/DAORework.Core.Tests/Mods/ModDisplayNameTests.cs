using DAORework.Core.Mods.Library;
using Xunit;

namespace DAORework.Core.Tests.Mods;

/// <summary>Names as a real library has them.</summary>
public sealed class ModDisplayNameTests
{
    [Theory]
    [InlineData("rules_core", "Rules core")]
    [InlineData("commoner--a little leg", "Commoner - a little leg")]
    [InlineData("Sleep_until_Dawn_bugfix2_2", "Sleep until Dawn bugfix2 2")]
    [InlineData("loose", "Loose files")]
    [InlineData("HAIRSTYLE PACK NEW", "HAIRSTYLE PACK NEW")]
    [InlineData("Extra Dog Slot v1.41", "Extra Dog Slot v1.41")]
    [InlineData("___", "___")]
    public void For_ReadsAsWords(string name, string expected)
    {
        Assert.Equal(expected, ModDisplayName.For(name));
    }
}
