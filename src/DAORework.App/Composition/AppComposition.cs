using DAORework.Application;
using DAORework.Infrastructure;
using DAORework.Infrastructure.KnownFolders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace DAORework.App.Composition;

/// <summary>The composition root (SDD §9.2): the same layers as the CLI, plus the view models.</summary>
public static class AppComposition
{
    public static ServiceProvider Build()
    {
        var appDataDirectory = Path.Combine(new EnvironmentKnownFolders().LocalApplicationData, "DAORework");
        Log.Logger = AppLogging.Create(Path.Combine(appDataDirectory, "logs"));

        return new ServiceCollection()
            .AddLogging(builder => builder.ClearProviders().AddSerilog(dispose: true))
            .AddApplication()
            .AddInfrastructure(options => options.AppDataDirectory = appDataDirectory)
            .AddAppViewModels()
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }
}
