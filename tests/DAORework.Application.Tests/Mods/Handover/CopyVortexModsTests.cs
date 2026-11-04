using DAORework.Application.Abstractions;
using DAORework.Application.Mods.Handover;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Adoption;
using DAORework.Core.Mods.Handover;
using DAORework.Core.Mods.Library;
using Xunit;

namespace DAORework.Application.Tests.Mods.Handover;

/// <summary>A refusal stands: only a folder that can't be read falls back to the files Vortex placed (ADR-0028 §3).</summary>
public sealed class CopyVortexModsTests
{
    private const string Mod = "Hair-1-1";

    [Fact]
    public async Task Execute_ADetection_IsNotBypassedByTheFallback()
    {
        var importer = new CountingImporter();

        // The scan is null on purpose: reaching the fallback would use it.
        var usecase = new CopyVortexMods(new OneModReader(), new RefusingLibrary(), new NoStore(), importer, null!);
        var report = (await usecase.ExecuteAsync("docs")).Value;

        var failed = Assert.Single(report.Failed);
        Assert.Equal(DiagnosticCodes.ModSafetyThreatFound, failed.Error.Code);
        Assert.Equal(0, importer.Calls);
        Assert.Empty(report.Copies);
    }

    private sealed class OneModReader : IHandInstalledModsReader
    {
        public Task<Result<HandInstalledSnapshot>> ReadAsync(string documentsDirectory, CancellationToken cancellationToken = default)
        {
            var record = new VortexRecord([new VortexPlacedFile("packages/core/override/hair.dds", Mod)])
            {
                AtItsTarget = true,
                StagedMods = new HashSet<string> { Mod },
                StagingPath = "staging",
                RelativeFolder = "packages/core/override/",
            };
            var none = new Dictionary<string, IReadOnlyList<HandInstalledFile>>();
            return Task.FromResult(Result.Success(new HandInstalledSnapshot([], none, none, []) { VortexRecords = [record] }));
        }
    }

    private sealed class RefusingLibrary : IModLibrary
    {
        public Task<Result<LibraryImport>> ImportAsync(string sourcePath, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Failure<LibraryImport>(new Error(DiagnosticCodes.ModSafetyThreatFound, "detected")));

        public Task<IReadOnlyList<LibraryEntry>> ListAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<LibraryEntry>>([]);

        public Task<Result<string>> ReadTextAsync(string modId, string relativePath, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<Result<LibraryModFile>> StoreCombinedAsync(string fileName, string text, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class NoStore : IVortexHandoverStore
    {
        public Task<Result<VortexHandover?>> LoadAsync(string documentsDirectory, CancellationToken cancellationToken = default) => Task.FromResult(Result.Success<VortexHandover?>(null));

        public Task SaveAsync(string documentsDirectory, VortexHandover handover, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task KeepAddInsListAsync(string documentsDirectory, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class CountingImporter : IAdoptionImporter
    {
        public int Calls { get; private set; }

        public Task<Result<LibraryEntry>> ImportAsync(string documentsDirectory, AdoptionCandidate candidate, string name, CancellationToken cancellationToken = default)
        {
            Calls++;
            throw new NotSupportedException();
        }
    }
}
