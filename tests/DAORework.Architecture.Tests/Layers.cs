using System.Reflection;

namespace DAORework.Architecture.Tests;

/// <summary>The four production assemblies, resolved once for every rule.</summary>
internal static class Layers
{
    public const string CoreName = "DAORework.Core";
    public const string ApplicationName = "DAORework.Application";
    public const string InfrastructureName = "DAORework.Infrastructure";
    public const string CliName = "DAORework.Cli";
    public const string AppName = "DAORework.App";

    public static readonly Assembly Core = typeof(DAORework.Core.Diagnostics.DiagnosticCodes).Assembly;
    public static readonly Assembly Application = typeof(DAORework.Application.Diagnostics.RunDiagnostics).Assembly;
    public static readonly Assembly Infrastructure = typeof(DAORework.Infrastructure.Persistence.JsonProfileRepository).Assembly;
    public static readonly Assembly App = typeof(DAORework.App.App).Assembly;

    public static IReadOnlyList<string> ReferencedAssemblyNames(Assembly assembly) =>
        assembly.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty).Order().ToList();

    public static bool IsBcl(string assemblyName) =>
        assemblyName == "netstandard" || assemblyName == "mscorlib" || assemblyName.StartsWith("System", StringComparison.Ordinal);
}
