using DAORework.Core.Mods.Library;
using Xunit;

namespace DAORework.Core.Tests.Mods.Programs;

/// <summary>P2.8-04 (ADR-0033 §1): where each file of a mod that runs inside the game goes.</summary>
public sealed class GameProgramTargetsTests
{
    [Fact]
    public void Targets_ABinShipFolder_GoesThereAndTheRestStays()
    {
        var targets = GameProgramLayout.Targets(Files("DAO Lib/bin_ship/dinput8.dll", "DAO Lib/bin_ship/daolib/addresses/1.05.txt", "DAO Lib/readme.txt"));

        Assert.Equal(["bin_ship/dinput8.dll", "bin_ship/daolib/addresses/1.05.txt"], targets.Select(t => t.TargetPath));
    }

    [Fact]
    public void Targets_DllsAtTheRoot_EverythingGoesIntoBinShipAsPacked()
    {
        var targets = GameProgramLayout.Targets(Files("Luma/d3d9.dll", "Luma/Luma-DAO.addon", "Luma/Luma/shaders/tonemap.hlsl"));

        Assert.Equal(["bin_ship/d3d9.dll", "bin_ship/Luma-DAO.addon", "bin_ship/Luma/shaders/tonemap.hlsl"], targets.Select(t => t.TargetPath));
    }

    [Fact]
    public void Targets_NoFiles_IsEmpty()
    {
        Assert.Empty(GameProgramLayout.Targets([]));
    }

    private static List<LibraryModFile> Files(params string[] paths) => [.. paths.Select(p => new LibraryModFile(p, 1, "h"))];
}
