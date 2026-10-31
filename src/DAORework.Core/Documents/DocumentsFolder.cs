namespace DAORework.Core.Documents;

/// <summary>A candidate <c>Documents\BioWare\Dragon Age</c> folder as it was found at detection time (data, not live checks).</summary>
public sealed record DocumentsFolder(string Path, DocumentsFolderSource Source, bool Exists, bool IsOneDrive);
