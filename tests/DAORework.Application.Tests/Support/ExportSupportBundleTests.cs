using System.IO.Abstractions.TestingHelpers;
using DAORework.Application.Abstractions;
using DAORework.Application.Diagnostics;
using DAORework.Application.Mods;
using DAORework.Application.Profiles;
using DAORework.Application.Support;
using DAORework.Application.Tests.Fakes;
using DAORework.Core.Diagnostics;
using DAORework.Core.Profiles;
using DAORework.Core.Support;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace DAORework.Application.Tests.Support;

public sealed class ExportSupportBundleTests
{
    private readonly MockFileSystem _fileSystem = new();
    private readonly InMemoryProfileRepository _repository = new();
    private readonly FakeKnownFolders _knownFolders = new();
    private readonly CapturingWriter _writer = new();

    [Fact]
    public async Task ExecuteAsync_UnknownProfile_FailsWithoutWriting()
    {
        var result = await CreateUseCase().ExecuteAsync(new ExportSupportBundleRequest("Nope"));

        Assert.Equal(DiagnosticCodes.ProfileNotFound, result.Error!.Code);
        Assert.Null(_writer.Bundle);
    }

    [Fact]
    public async Task ExecuteAsync_KnownProfile_WritesProfileDiagnosticsAndInventory()
    {
        _repository.Profiles["A"] = new RuntimeProfile { Name = "A", UserDocumentsDirectory = TestPaths.Root("Docs") };

        var result = await CreateUseCase().ExecuteAsync(new ExportSupportBundleRequest("A"));

        Assert.Equal(CapturingWriter.Path, result.Value);
        var bundle = _writer.Bundle!;
        Assert.Equal("A", bundle.Profile.Name);
        Assert.Equal("A", bundle.Diagnostics.ProfileName);
        Assert.NotEmpty(bundle.Diagnostics.Issues);
        Assert.NotNull(bundle.Inventory);
        Assert.False(bundle.Inventory.IncludesHashes);
        Assert.Null(bundle.InventoryError);
        Assert.Contains(PathRedactor.Placeholder, bundle.Redactor.Redact(_knownFolders.UserProfile), StringComparison.Ordinal);
    }

    private ExportSupportBundle CreateUseCase()
    {
        var time = new FakeTimeProvider();
        return new ExportSupportBundle(
            new GetProfile(_repository),
            new RunDiagnostics(TestDiagnosticRules.All(_fileSystem, _knownFolders), time),
            new BuildModInventory(_fileSystem, new FakeFileHasher(), _knownFolders, time, NullLogger<BuildModInventory>.Instance),
            _writer,
            _knownFolders);
    }

    private sealed class CapturingWriter : ISupportBundleWriter
    {
        public const string Path = "bundle.zip";

        public SupportBundle? Bundle { get; private set; }

        public Task<string> WriteAsync(SupportBundle bundle, CancellationToken cancellationToken = default)
        {
            Bundle = bundle;
            return Task.FromResult(Path);
        }
    }
}
