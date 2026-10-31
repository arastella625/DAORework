using DAORework.Application;
using DAORework.Application.Diagnostics;
using DAORework.Application.Installations;
using DAORework.Application.Launching;
using DAORework.Application.Mods;
using DAORework.Application.Profiles;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DAORework.Infrastructure.Tests;

/// <summary>Builds the real container the way Program.cs does, so a missing registration fails here and not at first use.</summary>
public sealed class DependencyInjectionTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "DAORework.Tests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Theory]
    [InlineData(typeof(DetectInstallations))]
    [InlineData(typeof(CreateProfile))]
    [InlineData(typeof(GetProfile))]
    [InlineData(typeof(ListProfiles))]
    [InlineData(typeof(UpdateLaunchTarget))]
    [InlineData(typeof(UpdateProfilePath))]
    [InlineData(typeof(RunDiagnostics))]
    [InlineData(typeof(InspectModFolders))]
    [InlineData(typeof(BuildModInventory))]
    [InlineData(typeof(LaunchGame))]
    public void EveryUseCase_ResolvesFromTheComposedContainer(Type useCase)
    {
        using var services = new ServiceCollection()
            .AddLogging()
            .AddApplication()
            .AddInfrastructure(options => options.AppDataDirectory = _directory)
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        Assert.NotNull(services.GetRequiredService(useCase));
    }
}
