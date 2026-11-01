using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DAORework.Application.Abstractions;
using DAORework.Application.Diagnostics;
using DAORework.Application.Profiles;
using DAORework.Application.Support;
using DAORework.Core.Diagnostics;
using Microsoft.Extensions.Logging;

namespace DAORework.App.ViewModels;

/// <summary>The diagnostics page: runs the rules for the selected profile and lists the issues; the report goes to disk as in the CLI.</summary>
public sealed partial class DiagnosticsPageViewModel : PageViewModel
{
    private readonly GetProfile _getProfile;
    private readonly RunDiagnostics _runDiagnostics;
    private readonly IDiagnosticReportWriter _reportWriter;
    private readonly ExportSupportBundle _exportSupportBundle;

    public DiagnosticsPageViewModel(
        GetProfile getProfile,
        RunDiagnostics runDiagnostics,
        IDiagnosticReportWriter reportWriter,
        ExportSupportBundle exportSupportBundle,
        ILogger<DiagnosticsPageViewModel> logger)
        : base(logger)
    {
        _getProfile = getProfile;
        _runDiagnostics = runDiagnostics;
        _reportWriter = reportWriter;
        _exportSupportBundle = exportSupportBundle;
    }

    public ObservableCollection<DiagnosticIssue> Issues { get; } = [];

    /// <summary>Gets or sets what runs once a report is shown, such as working out the next steps (P2.5-07).</summary>
    public Func<string, IReadOnlyList<DiagnosticIssue>, CancellationToken, Task>? AfterReportAsync { get; set; }

    [ObservableProperty]
    public partial string? ProfileName { get; set; }

    [ObservableProperty]
    public partial bool HasReport { get; set; }

    [ObservableProperty]
    public partial bool CanLaunch { get; set; }

    [ObservableProperty]
    public partial string? ReportPath { get; set; }

    [ObservableProperty]
    public partial string? BundlePath { get; set; }

    public void Load(string? profileName)
    {
        ProfileName = profileName;
        Issues.Clear();
        HasReport = false;
        CanLaunch = false;
        ReportPath = null;
        BundlePath = null;
        ReportSuccess(string.Empty);
    }

    [RelayCommand]
    private Task ExportSupportBundleAsync(CancellationToken cancellationToken) =>
        GuardAsync(
            async token =>
            {
                if (ProfileName is null)
                {
                    return;
                }

                var result = await _exportSupportBundle.ExecuteAsync(new ExportSupportBundleRequest(ProfileName), token);
                if (!result.TryGetValue(out var path, out var error))
                {
                    ReportError(error);
                    return;
                }

                BundlePath = path;
                ReportStatus("Support bundle written. Your user name is replaced with <user>; check the contents before sharing it.");
            },
            cancellationToken);

    [RelayCommand]
    private Task RunDiagnosticsAsync(CancellationToken cancellationToken) =>
        GuardAsync(
            async token =>
            {
                if (ProfileName is null)
                {
                    return;
                }

                var profileResult = await _getProfile.ExecuteAsync(new GetProfileRequest(ProfileName), token);
                if (!profileResult.TryGetValue(out var profile, out var error))
                {
                    ReportError(error);
                    return;
                }

                var report = (await _runDiagnostics.ExecuteAsync(new RunDiagnosticsRequest(profile), token)).Value;
                ReportPath = await _reportWriter.WriteAsync(report, token);

                Issues.Clear();
                foreach (var issue in report.Issues)
                {
                    Issues.Add(issue);
                }

                CanLaunch = report.CanLaunch;
                HasReport = true;
                ReportStatus(report.CanLaunch ? "The profile can launch." : "The profile cannot launch until the errors are fixed.");
                if (AfterReportAsync is not null)
                {
                    await AfterReportAsync(ProfileName, report.Issues, token);
                }
            },
            cancellationToken);
}
