using System.IO.Abstractions;
using System.Xml;
using System.Xml.Linq;
using DAORework.Core.Catalog;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Profiles;

namespace DAORework.Application.Catalog.Matchers;

/// <summary>Finds an add-in by the UID its <c>.dazip</c> registered in <c>Settings\AddIns.xml</c>, enabled or not.</summary>
public sealed class AddInUidMatcher : IFingerprintMatcher
{
    private readonly IFileSystem _fileSystem;

    public AddInUidMatcher(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public FingerprintKind Kind => FingerprintKind.AddInUid;

    public Task<Result<IReadOnlyList<FingerprintMatch>>> MatchAsync(
        KnownItemFingerprint fingerprint,
        RuntimeProfile profile,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fingerprint);
        ArgumentNullException.ThrowIfNull(profile);
        var documents = profile.UserDocumentsDirectory;
        if (string.IsNullOrWhiteSpace(documents) || !_fileSystem.File.Exists(GamePaths.AddInsListFile(documents)))
        {
            return Task.FromResult(Matched([]));
        }

        return Task.FromResult(Match(GamePaths.AddInsListFile(documents), fingerprint.Value));
    }

    private Result<IReadOnlyList<FingerprintMatch>> Match(string path, string uid)
    {
        List<XElement> items;
        try
        {
            items = XDocument.Parse(_fileSystem.File.ReadAllText(path))
                .Descendants("AddInItem")
                .Where(item => string.Equals((string?)item.Attribute("UID"), uid, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
        catch (XmlException exception)
        {
            return Unreadable(path, exception.Message);
        }
        catch (IOException exception)
        {
            return Unreadable(path, exception.Message);
        }

        return Matched(items
            .Select(item => new FingerprintMatch((string?)item.Attribute("Enabled") == "1"
                ? $"add-in {uid} in AddIns.xml"
                : $"add-in {uid} in AddIns.xml, disabled"))
            .ToList());
    }

    private static Result<IReadOnlyList<FingerprintMatch>> Matched(IReadOnlyList<FingerprintMatch> matches) => Result.Success(matches);

    private static Result<IReadOnlyList<FingerprintMatch>> Unreadable(string path, string reason) =>
        Result.Failure<IReadOnlyList<FingerprintMatch>>(new Error(DiagnosticCodes.KnownItemUnreadable, $"{path} could not be read ({reason})."));
}
