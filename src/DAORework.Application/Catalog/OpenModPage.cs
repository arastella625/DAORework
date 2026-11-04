using DAORework.Application.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;

namespace DAORework.Application.Catalog;

/// <summary>
/// Opens a known item's page in the player's default browser (ADR-0034 §7, ROADMAP P3-11). Only <c>https</c> links open,
/// so nothing in the catalog can start another program through the shell.
/// </summary>
public sealed class OpenModPage
{
    private readonly IUriLauncher _launcher;

    public OpenModPage(IUriLauncher launcher)
    {
        _launcher = launcher;
    }

    public Result Execute(string? link) =>
        Uri.TryCreate(link, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps
            ? _launcher.Open(uri)
            : Result.Failure(new Error(DiagnosticCodes.ModPageLinkRefused, $"Couldn't open that page: {link} isn't an https link."));
}
