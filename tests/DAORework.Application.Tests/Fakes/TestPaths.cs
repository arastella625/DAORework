using System.IO.Abstractions.TestingHelpers;

namespace DAORework.Application.Tests.Fakes;

/// <summary>Rooted test paths that read as Windows paths but work on the current OS.</summary>
internal static class TestPaths
{
    public static string Root(params string[] segments) => MockUnixSupport.Path(@"C:\" + string.Join(@"\", segments));
}
