using NetArchTest.Rules;
using Xunit;

namespace DAORework.Architecture.Tests;

/// <summary>Design rules from ENGINEERING_STANDARDS §3: sealed by default.</summary>
public sealed class DesignRuleTests
{
    [Theory]
    [InlineData(Layers.CoreName)]
    [InlineData(Layers.ApplicationName)]
    [InlineData(Layers.InfrastructureName)]
    public void PublicClasses_AreSealedAbstractOrStatic(string assemblyName)
    {
        var assembly = assemblyName switch
        {
            Layers.CoreName => Layers.Core,
            Layers.ApplicationName => Layers.Application,
            _ => Layers.Infrastructure,
        };

        var result = Types.InAssembly(assembly)
            .That().AreClasses().And().ArePublic().And().AreNotStatic().And().AreNotAbstract()
            .Should().BeSealed()
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join(", ", (result.FailingTypes ?? []).Select(t => t.FullName)));
    }
}
