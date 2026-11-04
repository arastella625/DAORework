namespace DAORework.Core.Mods.Downloads;

/// <summary>A file to offer: the file itself and the name to show, leaving off the Nexus id and version.</summary>
public sealed record NewDownload(DownloadFile File, string Name);
