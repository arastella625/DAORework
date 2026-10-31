using DAORework.Application.Abstractions;
using DAORework.Application.Diagnostics;
using DAORework.Application.Profiles;
using DAORework.Cli.Commands.Profiles;
using DAORework.Cli.Composition;
using DAORework.Cli.Rendering;
using Spectre.Console.Cli;

namespace DAORework.Cli.Commands;

public sealed class DiagnoseCommand : AsyncCommand<ProfileNameSettings>
{
    private readonly GetProfile _getProfile;
    private readonly RunDiagnostics _runDiagnostics;
    private readonly IDiagnosticReportWriter _reportWriter;
    private readonly DiagnosticReportRenderer _renderer;
    private readonly ErrorRenderer _errors;

    public DiagnoseCommand(
        GetProfile getProfile,
        RunDiagnostics runDiagnostics,
        IDiagnosticReportWriter reportWriter,
        DiagnosticReportRenderer renderer,
        ErrorRenderer errors)
    {
        _getProfile = getProfile;
        _runDiagnostics = runDiagnostics;
        _reportWriter = reportWriter;
        _renderer = renderer;
        _errors = errors;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, ProfileNameSettings settings, CancellationToken cancellationToken)
    {
        if (!(await _getProfile.ExecuteAsync(new GetProfileRequest(settings.Profile), cancellationToken)).TryGetValue(out var profile, out var error))
        {
            _errors.Render(error);
            return ExitCodes.Failure;
        }

        var report = (await _runDiagnostics.ExecuteAsync(new RunDiagnosticsRequest(profile), cancellationToken)).Value;
        var reportPath = await _reportWriter.WriteAsync(report, cancellationToken);
        _renderer.Render(report, reportPath);
        return ExitCodes.Success;
    }
}
