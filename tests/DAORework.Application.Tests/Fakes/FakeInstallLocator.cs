using DAORework.Application.Installations;
using DAORework.Core.Installations;

namespace DAORework.Application.Tests.Fakes;

internal sealed class FakeInstallLocator : IInstallLocator
{
    public FakeInstallLocator(params Installation[] installations)
    {
        Installations = installations.ToList();
    }

    public List<Installation> Installations { get; }

    public Task<IReadOnlyList<Installation>> LocateAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Installation>>(Installations);
}
