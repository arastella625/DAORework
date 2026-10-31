using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using DAORework.Application;
using DAORework.Application.Abstractions;
using DAORework.Cli.Composition;
using DAORework.Cli.Tests.Fakes;
using DAORework.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Spectre.Console.Cli.Testing;

namespace DAORework.Cli.Tests;

/// <summary>
/// The real composition (Program.cs minus Serilog) with the machine ports swapped for fakes:
/// an in-memory file system for the game and mod folders, fake known folders, and a fake process launcher.
/// Profiles and reports still go through the real JSON adapters into a temp directory.
/// </summary>
internal sealed class CliTestHost : IDisposable
{
    private readonly string _appData = Path.Combine(Path.GetTempPath(), "DAORework.Tests", Guid.NewGuid().ToString("N"));

    public MockFileSystem FileSystem { get; } = new();

    public FakeKnownFolders KnownFolders { get; } = new();

    public FakeProcessLauncher ProcessLauncher { get; } = new();

    public CommandAppResult Run(params string[] args)
    {
        var services = new ServiceCollection()
            .AddLogging()
            .AddApplication()
            .AddInfrastructure(options => options.AppDataDirectory = _appData)
            .AddCliRendering();
        services.Replace(ServiceDescriptor.Singleton<IFileSystem>(FileSystem));
        services.Replace(ServiceDescriptor.Singleton<IKnownFolders>(KnownFolders));
        services.Replace(ServiceDescriptor.Singleton<IProcessLauncher>(ProcessLauncher));
        services.Replace(ServiceDescriptor.Singleton<IRegistry>(new EmptyRegistry()));

        var tester = new CommandAppTester(new MicrosoftDependencyInjectionRegistrar(services));
        tester.Configure(CliConfiguration.Configure);
        return tester.Run(args);
    }

    public void Dispose()
    {
        if (Directory.Exists(_appData))
        {
            Directory.Delete(_appData, recursive: true);
        }
    }
}
