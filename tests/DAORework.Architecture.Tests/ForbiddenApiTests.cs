using NetArchTest.Rules;
using Xunit;

namespace DAORework.Architecture.Tests;

/// <summary>
/// No static I/O, registry, process or environment access outside Infrastructure
/// (SDD §4.2, ENGINEERING_STANDARDS §2). Time access (DateTime.Now) is a member of a
/// struct the models legitimately use, so it is enforced by review, not here.
/// </summary>
public sealed class ForbiddenApiTests
{
    private static readonly string[] _forbiddenInLogicLayers =
    [
        "System.IO.File",
        "System.IO.Directory",
        "System.IO.FileInfo",
        "System.IO.DirectoryInfo",
        "System.IO.FileSystemInfo",
        "Microsoft.Win32.Registry",
        "System.Diagnostics.Process",
        "System.Environment",
    ];

    [Fact]
    public void Core_DoesNotUseForbiddenApis()
    {
        var result = Types.InAssembly(Layers.Core).ShouldNot().HaveDependencyOnAny(_forbiddenInLogicLayers).GetResult();

        Assert.True(result.IsSuccessful, Explain(result));
    }

    [Fact]
    public void Application_DoesNotUseForbiddenApis()
    {
        var result = Types.InAssembly(Layers.Application).ShouldNot().HaveDependencyOnAny(_forbiddenInLogicLayers).GetResult();

        Assert.True(result.IsSuccessful, Explain(result));
    }

    private static string Explain(TestResult result) =>
        string.Join(Environment.NewLine, (result.FailingTypes ?? []).Select(t => $"{t.FullName}: {t.Explanation}"));
}
