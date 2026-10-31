using DAORework.Application.Documents;
using DAORework.Application.Installations;
using DAORework.Cli.Composition;
using DAORework.Cli.Rendering;
using Spectre.Console.Cli;

namespace DAORework.Cli.Commands;

public sealed class DetectCommand : AsyncCommand<DetectSettings>
{
    private readonly DetectInstallations _detectInstallations;
    private readonly DetectDocumentsFolders _detectDocumentsFolders;
    private readonly InstallationRenderer _installations;
    private readonly DocumentsFolderRenderer _documents;

    public DetectCommand(
        DetectInstallations detectInstallations,
        DetectDocumentsFolders detectDocumentsFolders,
        InstallationRenderer installations,
        DocumentsFolderRenderer documents)
    {
        _detectInstallations = detectInstallations;
        _detectDocumentsFolders = detectDocumentsFolders;
        _installations = installations;
        _documents = documents;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, DetectSettings settings, CancellationToken cancellationToken)
    {
        var request = new DetectInstallationsRequest(settings.Paths);
        var installations = (await _detectInstallations.ExecuteAsync(request, cancellationToken)).Value;
        _installations.Render(installations);

        var documents = (await _detectDocumentsFolders.ExecuteAsync(new DetectDocumentsFoldersRequest(), cancellationToken)).Value;
        _documents.Render(documents);
        return ExitCodes.Success;
    }
}
