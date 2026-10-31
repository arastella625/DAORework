using System.Text.RegularExpressions;
using Xunit;

namespace DAORework.App.Tests.Themes;

/// <summary>
/// ADR-0016 rule 1: no color outside a theme. Every view takes its colors from the theme tokens, so a page cannot drift
/// from the theme or skip the contrast tests. Reads the App's sources, which tests may do: they are not a layer.
/// </summary>
public sealed partial class ViewsUseTokensTests
{
    [Fact]
    public void NoView_NamesAColorOrAFluentBrush()
    {
        var views = Path.Combine(RepositoryRoot(), "src", "DAORework.App", "Views");
        var offenders = Directory.EnumerateFiles(views, "*.*", SearchOption.AllDirectories)
            .Where(file => file.EndsWith(".axaml", StringComparison.Ordinal) || file.EndsWith(".cs", StringComparison.Ordinal))
            .SelectMany(file => File.ReadLines(file).Select((line, index) => (file, line, index)))
            .Where(entry => ColorLiteral().IsMatch(entry.line))
            .Select(entry => $"{Path.GetFileName(entry.file)}:{entry.index + 1}: {entry.line.Trim()}")
            .ToList();

        Assert.True(offenders.Count == 0, "Use a theme token instead:\n" + string.Join('\n', offenders));
    }

    private static string RepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "DAORework.sln")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("DAORework.sln was not found above the test output folder.");
    }

    // A hex color ("#C42B1C"), a color built in code, a brush made in a view, or one of Fluent's own brushes.
    [GeneratedRegex(@"""#[0-9A-Fa-f]{3,8}""|Color\.From|Colors\.|new SolidColorBrush|SystemControl\w*Brush")]
    private static partial Regex ColorLiteral();
}
