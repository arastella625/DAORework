using DAORework.Core.Mods.Backups;
using Xunit;

namespace DAORework.Core.Tests.Mods;

public sealed class DocumentsKeyTests
{
    [Fact]
    public void For_SameFolderWrittenDifferently_GivesTheSameKey()
    {
        var key = DocumentsKey.For(@"C:\Users\Me\Documents\BioWare\Dragon Age");

        Assert.Equal(key, DocumentsKey.For("c:/users/me/documents/bioware/dragon age/"));
        Assert.Equal(16, key.Length);
        Assert.Matches("^[0-9a-f]{16}$", key);
    }

    [Fact]
    public void For_DifferentFolders_GiveDifferentKeys()
    {
        Assert.NotEqual(DocumentsKey.For(@"C:\Users\Me\Documents\BioWare\Dragon Age"), DocumentsKey.For(@"C:\Users\Me\OneDrive\Documents\BioWare\Dragon Age"));
    }
}
