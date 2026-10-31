using System.IO.Abstractions;
using DAORework.Application.Common;
using DAORework.Core.Mods;
using DAORework.Infrastructure.Persistence;
using Microsoft.Extensions.Options;
using Xunit;

namespace DAORework.Infrastructure.Tests.Persistence;

public sealed class JsonDaoModProfileServiceTests : IDisposable
{
    private readonly TempDirectory _temp = new();

    public void Dispose() => _temp.Dispose();

    [Fact]
    public async Task SaveThenList_RoundTripsAndStampsTheSchemaVersion()
    {
        var service = CreateService();
        var profile = new DaoModProfile { Name = "Mods", Description = "d", EnabledOverrideDirectories = ["a"], EnabledPackagePaths = ["p"] };

        await service.SaveAsync(profile);
        var listed = await service.ListAsync();

        var loaded = Assert.Single(listed);
        Assert.Equal(profile.Name, loaded.Name);
        Assert.Equal(profile.Description, loaded.Description);
        Assert.Equal(profile.EnabledOverrideDirectories, loaded.EnabledOverrideDirectories);
        Assert.Equal(profile.EnabledPackagePaths, loaded.EnabledPackagePaths);
        Assert.Contains($"\"schemaVersion\": {DaoModProfile.CurrentSchemaVersion}", await File.ReadAllTextAsync(_temp.File("mod-profiles/Mods.json")), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ListAsync_LegacyDocument_TakesItsNameFromTheFile()
    {
        var service = CreateService();
        await File.WriteAllTextAsync(_temp.File("mod-profiles/My_Mods.json"), """{"name":"My:Mods","enabledOverrideDirectories":["a"]}""");

        var loaded = Assert.Single(await service.ListAsync());

        Assert.Equal("My_Mods", loaded.Name);
        Assert.Equal(DaoModProfile.CurrentSchemaVersion, loaded.SchemaVersion);
        Assert.Equal(["a"], loaded.EnabledOverrideDirectories);
    }

    [Fact]
    public async Task SaveAsync_InvalidName_Throws()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentException>(() => service.SaveAsync(new DaoModProfile { Name = "bad*name" }));
    }

    private JsonDaoModProfileService CreateService() =>
        new(new FileSystem(), Options.Create(new DaoReworkOptions { AppDataDirectory = _temp.Path }));
}
