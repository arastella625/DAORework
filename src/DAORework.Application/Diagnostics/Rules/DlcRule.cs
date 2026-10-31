using System.IO.Abstractions;
using System.Xml;
using System.Xml.Linq;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Profiles;

namespace DAORework.Application.Diagnostics.Rules;

/// <summary>The add-ins (DLC and add-in mods) the game has registered in <c>Settings\AddIns.xml</c>, reported by title.</summary>
public sealed class DlcRule : IDiagnosticRule
{
    private readonly IFileSystem _fileSystem;

    public DlcRule(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public Task<IReadOnlyList<DiagnosticIssue>> EvaluateAsync(RuntimeProfile profile, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var documents = profile.UserDocumentsDirectory;
        if (string.IsNullOrWhiteSpace(documents) || !_fileSystem.Directory.Exists(documents))
        {
            return Task.FromResult<IReadOnlyList<DiagnosticIssue>>([]);
        }

        var path = GamePaths.AddInsListFile(documents);
        if (!_fileSystem.File.Exists(path))
        {
            return Task.FromResult<IReadOnlyList<DiagnosticIssue>>(
                [new DiagnosticIssue(DiagnosticSeverity.Info, DiagnosticCodes.DlcNone, "No add-ins registered (no Settings\\AddIns.xml yet).")]);
        }

        return Task.FromResult<IReadOnlyList<DiagnosticIssue>>(Inspect(path));
    }

    private IReadOnlyList<DiagnosticIssue> Inspect(string path)
    {
        List<(string Title, bool Enabled)> addIns;
        try
        {
            addIns = XDocument.Parse(_fileSystem.File.ReadAllText(path))
                .Descendants("AddInItem")
                .Select(item => (Title(item), (string?)item.Attribute("Enabled") == "1"))
                .ToList();
        }
        catch (XmlException exception)
        {
            return [Unreadable(path, exception.Message)];
        }
        catch (IOException exception)
        {
            return [Unreadable(path, exception.Message)];
        }

        if (addIns.Count == 0)
        {
            return [new DiagnosticIssue(DiagnosticSeverity.Info, DiagnosticCodes.DlcNone, "No add-ins are registered with the game.")];
        }

        var enabled = addIns.Where(a => a.Enabled).Select(a => a.Title).Order(StringComparer.OrdinalIgnoreCase).ToList();
        var disabledCount = addIns.Count - enabled.Count;
        var summary = enabled.Count == 0 ? "none enabled" : string.Join(", ", enabled);
        var suffix = disabledCount == 0 ? string.Empty : $" ({disabledCount} disabled)";
        return [new DiagnosticIssue(DiagnosticSeverity.Info, DiagnosticCodes.DlcFound, $"{addIns.Count} add-in(s) registered: {summary}{suffix}.")];
    }

    private static string Title(XElement item) =>
        (string?)item.Element("Title")?.Attribute("DefaultText")
        ?? (string?)item.Attribute("Name")
        ?? (string?)item.Attribute("UID")
        ?? "(unnamed)";

    private static DiagnosticIssue Unreadable(string path, string reason) =>
        new(DiagnosticSeverity.Warning, DiagnosticCodes.DlcListUnreadable, $"Could not read the add-in list {path}: {reason}");
}
