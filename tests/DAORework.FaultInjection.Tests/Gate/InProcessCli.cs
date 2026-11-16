using DAORework.Application;
using DAORework.Application.Abstractions;
using DAORework.Application.Documents;
using DAORework.Cli.Composition;
using DAORework.FaultInjection.Tests.Host;
using DAORework.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Spectre.Console.Cli.Testing;

namespace DAORework.FaultInjection.Tests.Gate;

/// <summary>The CLI run in this process over the real file system, for setting up a run and settling it afterwards.</summary>
internal static class InProcessCli
{
    public static CliOutcome Run(string appData, IEnumerable<string> arguments)
    {
        var services = new ServiceCollection()
            .AddLogging()
            .AddApplication()
            .AddInfrastructure(options => options.AppDataDirectory = appData)
            .AddCliRendering();
        services.Replace(ServiceDescriptor.Singleton<IDocumentsFolderSearch, NoDocumentsFolderSearch>());
        services.Replace(ServiceDescriptor.Singleton<IMalwareScanner, CleanScan>());
        var tester = new CommandAppTester(new MicrosoftDependencyInjectionRegistrar(services));
        tester.Configure(CliConfiguration.Configure);
        var result = tester.Run([.. arguments]);
        return new CliOutcome(result.ExitCode, result.Output);
    }
}
