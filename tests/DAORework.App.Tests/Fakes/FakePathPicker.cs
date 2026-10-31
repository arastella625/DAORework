using DAORework.App.Services;

namespace DAORework.App.Tests.Fakes;

/// <summary>Answers each dialog with the next queued path; an empty queue means the user cancelled.</summary>
internal sealed class FakePathPicker : IPathPicker
{
    private readonly Queue<string?> _answers = new();

    public List<string> Titles { get; } = [];

    public void Enqueue(string? path) => _answers.Enqueue(path);

    public Task<string?> PickFolderAsync(string title, CancellationToken cancellationToken = default) => AnswerAsync(title);

    public Task<string?> PickFileAsync(string title, string filterName, IReadOnlyList<string> patterns, CancellationToken cancellationToken = default) => AnswerAsync(title);

    private Task<string?> AnswerAsync(string title)
    {
        Titles.Add(title);
        return Task.FromResult(_answers.Count == 0 ? null : _answers.Dequeue());
    }
}
