using System.IO.Abstractions;
using DAORework.Application;
using DAORework.Cli.Composition;
using DAORework.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Spectre.Console.Cli;

namespace DAORework.FaultInjection.Tests.Host;

/// <summary>Builds the CLI as Program.cs does, with the file system wrapped so the process kills itself before write number killAt.</summary>
internal static class FaultHost
{
    public const string Verb = "host";
    public const string WritesPrefix = "WRITES=";

    public static int Run(int killAt, string appData, string[] cliArguments)
    {
        var fileSystem = new KillingFileSystem(new FileSystem(), killAt);
        var services = new ServiceCollection()
            .AddLogging()
            .AddApplication()
            .AddInfrastructure(options => options.AppDataDirectory = appData)
            .AddCliRendering();
        services.Replace(ServiceDescriptor.Singleton<IFileSystem>(fileSystem));

        var app = new CommandApp(new MicrosoftDependencyInjectionRegistrar(services));
        app.Configure(config =>
        {
            CliConfiguration.Configure(config);
            config.ConfigureConsole(CliConsole.Create());
        });
        var exitCode = app.Run(cliArguments);
        Console.Error.WriteLine($"{WritesPrefix}{fileSystem.Writes}");
        return exitCode;
    }
}
