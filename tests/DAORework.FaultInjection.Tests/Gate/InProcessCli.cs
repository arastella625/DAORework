using DAORework.Application;
using DAORework.Cli.Composition;
using DAORework.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
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
        var tester = new CommandAppTester(new MicrosoftDependencyInjectionRegistrar(services));
        tester.Configure(CliConfiguration.Configure);
        var result = tester.Run([.. arguments]);
        return new CliOutcome(result.ExitCode, result.Output);
    }
}
