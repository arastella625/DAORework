using DAORework.Application.Mods.Library;
using DAORework.Core.Mods.Adoption;
using DAORework.Core.Mods.Library;

namespace DAORework.App.ViewModels;

/// <summary>One download held back from being added (ADR-0039): what it looks like, and whether <i>Manage it</i> can help.</summary>
public sealed class HeldDownloadRow
{
    public HeldDownloadRow(HeldDownload download)
    {
        ArgumentNullException.ThrowIfNull(download);
        Download = download;
    }

    public HeldDownload Download { get; }

    /// <summary>
    /// Gets a value indicating whether what it looks like is in the game and can be managed. Vortex would put back what a
    /// live mod of its moved, and loose files are managed all together, so that must be only this mod's.
    /// </summary>
    public bool CanManage => Download.Held is { InGame.DeployedByVortex: false } held
        && (held.NothingElse || held.InGame!.Kind != AdoptionCandidateKind.LooseOverrideFiles);

    /// <summary>Gets what the sheet says about it.</summary>
    public string Line
    {
        get
        {
            var name = ModDisplayName.For(Download.Name);
            return Download.Held.InGame switch
            {
                { Kind: AdoptionCandidateKind.LooseOverrideFiles } => $"{name} looks like it's already in your game, as loose files.",
                not null => $"{name} looks like {ModDisplayName.For(Download.Held.Name)}, which is already in your game.",
                null => $"{name} looks like {ModDisplayName.For(Download.Held.Name)}, which is already in your Library.",
            };
        }
    }
}
