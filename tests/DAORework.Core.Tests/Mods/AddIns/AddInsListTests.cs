using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.AddIns;
using Xunit;

namespace DAORework.Core.Tests.Mods.AddIns;

public sealed class AddInsListTests
{
    [Fact]
    public void Parse_KeepsEnabledEntriesWithAPriority_WithTheirPlaceInTheList()
    {
        var listed = Parse("""
            <AddInsList>
              <AddInItem UID="DAO_PRC_CP_2" Name="PRC_CP2_Shale" ExtendedModuleUID="Single Player" Priority="100" Enabled="1" BioWare="1"><Title DefaultText="The Stone Prisoner" /></AddInItem>
              <AddInItem UID="daorework_verify_change" Name="DAORework Verify Change" />
              <AddInItem UID="off" Priority="5" Enabled="0" />
              <AddInItem UID="qwinn_fixpack_3" Name="QwinnFixpack3" ExtendedModuleUID="Single Player" Priority="109" Enabled="1" />
            </AddInsList>
            """);

        Assert.Equal(
            [
                new ListedAddIn("DAO_PRC_CP_2", "The Stone Prisoner", "Single Player", 100, 0, true),
                new ListedAddIn("qwinn_fixpack_3", "QwinnFixpack3", "Single Player", 109, 3, false),
            ],
            listed);
    }

    [Fact]
    public void Parse_NoTitleOrName_UsesTheUid()
    {
        var listed = Assert.Single(Parse("""<AddInsList><AddInItem UID="bare" Priority="1" Enabled="1" /></AddInsList>"""));

        Assert.Equal("bare", listed.Title);
        Assert.Equal(string.Empty, listed.ExtendedModule);
    }

    [Fact]
    public void Parse_NotXml_IsAnErrorWithItsCode()
    {
        var result = AddInsList.Parse("<AddInsList>");

        Assert.False(result.IsSuccess);
        Assert.Equal(DiagnosticCodes.AddInsListUnreadable, result.Error!.Code);
    }

    private static IReadOnlyList<ListedAddIn> Parse(string xml) => AddInsList.Parse(xml).Value;
}
