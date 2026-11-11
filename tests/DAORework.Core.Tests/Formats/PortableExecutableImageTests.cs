using DAORework.Core.Formats;
using DAORework.Core.Tests.Mods.Programs;
using Xunit;

namespace DAORework.Core.Tests.Formats;

/// <summary>P2.8-04 (ADR-0033 §2): a DLL is told from a program that runs on its own by its header, not its name.</summary>
public sealed class PortableExecutableImageTests
{
    [Fact]
    public void TryRead_A32BitDll_SaysSo()
    {
        Assert.True(PortableExecutableImage.TryRead(TestPe.Build(), out var image));
        Assert.Equal(new PortableExecutableImage(PortableExecutableMachine.X86, IsDll: true), image);
    }

    [Fact]
    public void TryRead_AnExe_IsNotADll()
    {
        Assert.True(PortableExecutableImage.TryRead(TestPe.Build(TestPe.X64, dll: false), out var image));
        Assert.Equal(new PortableExecutableImage(PortableExecutableMachine.X64, IsDll: false), image);
    }

    [Fact]
    public void TryRead_NotAProgram_IsFalse()
    {
        Assert.False(PortableExecutableImage.TryRead("[settings]\nscale=2"u8, out var image));
        Assert.Null(image);
    }
}
