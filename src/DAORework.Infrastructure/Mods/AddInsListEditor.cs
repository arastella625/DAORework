using System.IO.Abstractions;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Planning;

namespace DAORework.Infrastructure.Mods;

/// <summary>
/// Adds and removes <c>AddInItem</c> entries in <c>Settings\AddIns.xml</c> and touches nothing else. An added entry is
/// the mod's own, copied from the <c>Manifest.xml</c> in its library folder, as DAUpdater does. Adding an entry that is
/// already there and removing one that is not both succeed, so the edit is safe to repeat.
/// </summary>
internal sealed class AddInsListEditor(IFileSystem fileSystem, string library)
{
    private const string ItemName = "AddInItem";
    private const string NewFileDeclaration = "<?xml version=\"1.0\" encoding=\"utf-8\"?>";

    public async Task<Result> EditAsync(string path, IReadOnlyList<AddInsListChange> changes, CancellationToken cancellationToken)
    {
        try
        {
            var (document, format) = await LoadAsync(path, cancellationToken);
            var root = document.Root;
            if (root is null || root.Name.LocalName != "AddInsList")
            {
                return Failed("Settings/AddIns.xml does not have an AddInsList at its root; it is left alone.");
            }

            foreach (var change in changes)
            {
                var existing = root.Elements(ItemName).Where(e => SameUid(e, change.Uid)).ToList();
                if (!change.Add)
                {
                    existing.ForEach(e => e.Remove());
                    continue;
                }

                if (existing.Count == 0)
                {
                    var item = await ManifestItemAsync(change, cancellationToken);
                    if (item is null)
                    {
                        return Failed($"The library copy of {change.ModId} has no AddInItem {change.Uid} in its Manifest.xml.");
                    }

                    root.Add(item);
                }
            }

            await SaveAsync(path, document, format, cancellationToken);
            return Result.Success();
        }
        catch (XmlException exception)
        {
            return Failed($"Settings/AddIns.xml could not be read: {exception.Message}");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return Failed($"Settings/AddIns.xml: {exception.Message}");
        }
    }

    private static Result Failed(string message) => Result.Failure(new Error(DiagnosticCodes.ModApplyFailed, message));

    /// <summary>The file's XML declaration exactly as written, or null when it has none; the writer would respell it.</summary>
    private static string? DeclarationOf(byte[] bytes, bool bom)
    {
        var text = Encoding.UTF8.GetString(bytes, bom ? Encoding.UTF8.Preamble.Length : 0, Math.Min(bytes.Length, 512) - (bom ? Encoding.UTF8.Preamble.Length : 0));
        var end = text.IndexOf("?>", StringComparison.Ordinal);
        return text.StartsWith("<?xml", StringComparison.Ordinal) && end > 0 ? text[..(end + 2)] : null;
    }

    private static bool SameUid(XElement item, string uid) => string.Equals((string?)item.Attribute("UID"), uid, StringComparison.OrdinalIgnoreCase);

    private async Task<(XDocument Document, FileFormat Format)> LoadAsync(string path, CancellationToken cancellationToken)
    {
        if (!fileSystem.File.Exists(path))
        {
            return (new XDocument(new XElement("AddInsList")), new FileFormat(Bom: false, NewLine: "\r\n", NewFileDeclaration));
        }

        var bytes = await fileSystem.File.ReadAllBytesAsync(path, cancellationToken);
        var bom = bytes.AsSpan().StartsWith(Encoding.UTF8.Preamble);

        // The parser turns every line break into \n, as XML requires, so the file's own is noted to write it back.
        var newLine = bytes.AsSpan().IndexOf("\r\n"u8) >= 0 ? "\r\n" : "\n";
        using var stream = new MemoryStream(bytes);
        return (XDocument.Load(stream, LoadOptions.PreserveWhitespace), new FileFormat(bom, newLine, DeclarationOf(bytes, bom)));
    }

    private async Task<XElement?> ManifestItemAsync(AddInsListChange change, CancellationToken cancellationToken)
    {
        var manifest = fileSystem.Path.Combine(library, change.ModId, "Manifest.xml");
        if (!fileSystem.File.Exists(manifest))
        {
            return null;
        }

        await using var stream = fileSystem.File.OpenRead(manifest);
        var document = await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken);
        var item = document.Descendants(ItemName).FirstOrDefault(e => SameUid(e, change.Uid));
        return item is null ? null : new XElement(item);
    }

    private async Task SaveAsync(string path, XDocument document, FileFormat format, CancellationToken cancellationToken)
    {
        fileSystem.Directory.CreateDirectory(fileSystem.Path.GetDirectoryName(path)!);
        var temp = path + DeploymentTempFiles.Suffix;
        var settings = new XmlWriterSettings
        {
            Async = true,
            Encoding = new UTF8Encoding(false),
            Indent = false,
            OmitXmlDeclaration = true,
            NewLineChars = format.NewLine,
            NewLineHandling = NewLineHandling.Replace,
        };
        await using (var stream = fileSystem.FileStream.New(temp, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await stream.WriteAsync(Encoding.UTF8.GetBytes((format.Bom ? "\uFEFF" : string.Empty) + format.Declaration), cancellationToken);
            await using var writer = XmlWriter.Create(stream, settings);
            await document.SaveAsync(writer, cancellationToken);
        }

        fileSystem.File.Move(temp, path, overwrite: true);
    }

    /// <summary>How the file was written, kept so an edit changes only the entries and not the bytes around them.</summary>
    private sealed record FileFormat(bool Bom, string NewLine, string? Declaration);
}
