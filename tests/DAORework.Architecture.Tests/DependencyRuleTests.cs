using Xunit;

namespace DAORework.Architecture.Tests;

/// <summary>The dependency rule from SDD §4.2, checked on assembly references.</summary>
public sealed class DependencyRuleTests
{
    /// <summary>The assemblies the System.IO.Abstractions package ships under (SDD §4.2 allows the package).</summary>
    private static readonly string[] _fileSystemAbstractionAssemblies =
    [
        "System.IO.Abstractions",
        "TestableIO.System.IO.Abstractions",
        "Testably.Abstractions.FileSystem.Interface",
    ];

    [Fact]
    public void Core_ReferencesOnlyTheBcl()
    {
        var offenders = Layers.ReferencedAssemblyNames(Layers.Core).Where(n => !Layers.IsBcl(n)).ToList();

        Assert.Empty(offenders);
    }

    [Fact]
    public void Application_DoesNotReferenceInfrastructureOrCli()
    {
        var references = Layers.ReferencedAssemblyNames(Layers.Application);

        Assert.DoesNotContain(Layers.InfrastructureName, references);
        Assert.DoesNotContain(Layers.CliName, references);
    }

    [Fact]
    public void Application_ReferencesOnlyCoreAbstractionsAndTheBcl()
    {
        var offenders = Layers.ReferencedAssemblyNames(Layers.Application)
            .Where(n => !Layers.IsBcl(n))
            .Where(n => n != Layers.CoreName)
            .Where(n => !(n.StartsWith("Microsoft.Extensions.", StringComparison.Ordinal) && n.EndsWith(".Abstractions", StringComparison.Ordinal)))
            .Where(n => !_fileSystemAbstractionAssemblies.Contains(n, StringComparer.Ordinal))
            .ToList();

        Assert.Empty(offenders);
    }

    [Fact]
    public void Infrastructure_DoesNotReferenceCliOrApp()
    {
        var references = Layers.ReferencedAssemblyNames(Layers.Infrastructure);

        Assert.DoesNotContain(Layers.CliName, references);
        Assert.DoesNotContain(Layers.AppName, references);
    }

    /// <summary>The two presentations are peers (ADR-0010): the GUI consumes the use cases, never the CLI.</summary>
    [Fact]
    public void App_DoesNotReferenceCli()
    {
        Assert.DoesNotContain(Layers.CliName, Layers.ReferencedAssemblyNames(Layers.App));
    }
}
