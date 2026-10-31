using DAORework.Application.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Looks;

namespace DAORework.Application.Tests.Fakes;

/// <summary>Imports nothing: returns the next result, the picture's bytes, and records what was removed.</summary>
internal sealed class FakeBackgroundImageImporter : IBackgroundImageImporter
{
    public Result<ImportedPicture> Next { get; set; } =
        Result.Success(new ImportedPicture("background.png", 1920, 1080, new LookColor(0xD8, 0xC9, 0xA0)));

    public Result<byte[]> Picture { get; set; } = Result.Success<byte[]>([1, 2, 3]);

    public List<(string LookId, string FileName)> Removed { get; } = [];

    public Task<Result<ImportedPicture>> ImportAsync(string sourcePath, string lookId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Next);

    public Task<Result<byte[]>> ReadAsync(string lookId, string fileName, CancellationToken cancellationToken = default) =>
        Task.FromResult(Picture);

    public Task RemoveAsync(string lookId, string fileName, CancellationToken cancellationToken = default)
    {
        Removed.Add((lookId, fileName));
        return Task.CompletedTask;
    }
}
