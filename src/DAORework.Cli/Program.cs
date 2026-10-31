using DAORework.Application;
using DAORework.Cli.Composition;
using DAORework.Infrastructure;
using DAORework.Infrastructure.KnownFolders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Spectre.Console.Cli;

// Composition root (SDD §9.1): build the host, run the command app, map failures to exit codes.
var appDataDirectory = Path.Combine(new EnvironmentKnownFolders().LocalApplicationData, "DAORework");
Log.Logger = CliLogging.Create(Path.Combine(appDataDirectory, "logs"));

var services = new ServiceCollection()
    .AddLogging(builder => builder.ClearProviders().AddSerilog(dispose: true))
    .AddApplication()
    .AddInfrastructure(options => options.AppDataDirectory = appDataDirectory)
    .AddCliRendering();

try
{
    var app = new CommandApp(new MicrosoftDependencyInjectionRegistrar(services));
    app.Configure(config =>
    {
        CliConfiguration.Configure(config);
        config.ConfigureConsole(CliConsole.Create());
    });
    return await app.RunAsync(args);
}
finally
{
    await Log.CloseAndFlushAsync();
}
