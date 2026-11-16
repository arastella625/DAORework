using DAORework.App.ViewModels;
using DAORework.Application.Abstractions;
using DAORework.Application.Mods.AddIns;
using DAORework.Application.Profiles;
using DAORework.Core.Common;
using DAORework.Core.Mods.AddIns;
using DAORework.Core.Profiles;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DAORework.App.Tests;

/// <summary>The add-in tags when the player picks another profile while the last one's add-ins are still being read (P2.5-23).</summary>
public sealed class AddInPriorityWhileSwitchingTests
{
    [Fact]
    public async Task Load_AnotherProfileBeforeTheFirstReadEnds_KeepsTheNewProfilesMarks()
    {
        var reader = new GatedReader();
        var profiles = new TwoProfiles();
        var addIns = new AddInPriorityViewModel(new GetProfile(profiles), new FindAddInClashes(reader), NullLogger<AddInPriorityViewModel>.Instance);

        var first = addIns.LoadAsync("A", TestContext.Current.CancellationToken);
        await addIns.LoadAsync("B", TestContext.Current.CancellationToken);
        reader.Release();
        await first;

        Assert.Equal(["b_mod"], addIns.Marks.Keys);
    }

    /// <summary>Profile A's add-ins are read only once <see cref="Release"/> is called; B's at once.</summary>
    private sealed class GatedReader : IDocumentsAddInsReader
    {
        private readonly TaskCompletionSource _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void Release() => _gate.SetResult();

        public async Task<Result<DocumentsAddIns>> ReadAsync(string documentsDirectory, CancellationToken cancellationToken = default)
        {
            if (documentsDirectory == "docs-a")
            {
                await _gate.Task.WaitAsync(cancellationToken);
            }

            var uid = documentsDirectory == "docs-a" ? "a_mod" : "b_mod";
            return Result.Success(new DocumentsAddIns([new ListedAddIn(uid, uid, "Single Player", 100, 0, IsOfficial: false)], [], []));
        }
    }

    private sealed class TwoProfiles : IProfileRepository
    {
        public Task<RuntimeProfile?> LoadAsync(string profileName, CancellationToken cancellationToken = default) =>
            Task.FromResult<RuntimeProfile?>(new RuntimeProfile { Name = profileName, UserDocumentsDirectory = profileName == "A" ? "docs-a" : "docs-b" });

        public Task SaveAsync(RuntimeProfile profile, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<string>> ListNamesAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<string>>(["A", "B"]);
    }
}
