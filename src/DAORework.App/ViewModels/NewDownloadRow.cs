using DAORework.Core.Common;
using DAORework.Core.Mods.Downloads;

namespace DAORework.App.ViewModels;

/// <summary>One file on the <i>New downloads</i> card (ADR-0034 §5): the mod's name, and which file it is.</summary>
public sealed record NewDownloadRow(NewDownload Download)
{
    public string Title => Download.Name;

    public string Details => $"{Download.File.FileName} · {new ByteSize(Download.File.SizeBytes)}";
}
