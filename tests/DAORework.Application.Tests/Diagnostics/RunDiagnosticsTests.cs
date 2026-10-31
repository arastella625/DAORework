using DAORework.Application.Diagnostics;
using DAORework.Application.Tests.Fakes;
using DAORework.Core.Diagnostics;
using DAORework.Core.Profiles;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace DAORework.Application.Tests.Diagnostics;

public sealed class RunDiagnosticsTests
{
    private static readonly DateTimeOffset _now = new(2026, 9, 29, 12, 0, 0, TimeSpan.Zero);
    private static readonly RuntimeProfile _profile = new() { Name = "P" };

    private static DiagnosticIssue Issue(DiagnosticSeverity severity, string code) => new(severity, code, code);

    [Fact]
    public async Task ExecuteAsync_NoRules_ReportsProfileValidWithTimestamp()
    {
        var useCase = new RunDiagnostics([], new FakeTimeProvider(_now));

        var result = await useCase.ExecuteAsync(new RunDiagnosticsRequest(_profile));

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.CanLaunch);
        Assert.Equal([DiagnosticCodes.ProfileValid], result.Value.Issues.Select(i => i.Code));
        Assert.Equal(_now, result.Value.CreatedAtUtc);
        Assert.Same(_profile, result.Value.ProfileSnapshot);
    }

    [Fact]
    public async Task ExecuteAsync_RulesRunInOrderAndIssuesAreConcatenated()
    {
        var first = new FakeDiagnosticRule(Issue(DiagnosticSeverity.Warning, "DAO_TEST_A"), Issue(DiagnosticSeverity.Info, "DAO_TEST_B"));
        var second = new FakeDiagnosticRule(Issue(DiagnosticSeverity.Warning, "DAO_TEST_C"));
        var useCase = new RunDiagnostics([first, second], new FakeTimeProvider(_now));

        var report = (await useCase.ExecuteAsync(new RunDiagnosticsRequest(_profile))).Value;

        Assert.Equal(["DAO_TEST_A", "DAO_TEST_B", "DAO_TEST_C", DiagnosticCodes.ProfileValid], report.Issues.Select(i => i.Code));
        Assert.Equal(1, first.Calls);
        Assert.Equal(1, second.Calls);
    }

    [Fact]
    public async Task ExecuteAsync_AnyErrorIssue_MeansCannotLaunchAndNoValidMarker()
    {
        var rule = new FakeDiagnosticRule(Issue(DiagnosticSeverity.Error, "DAO_TEST_FATAL"));
        var useCase = new RunDiagnostics([rule], new FakeTimeProvider(_now));

        var report = (await useCase.ExecuteAsync(new RunDiagnosticsRequest(_profile))).Value;

        Assert.False(report.CanLaunch);
        Assert.DoesNotContain(report.Issues, i => i.Code == DiagnosticCodes.ProfileValid);
    }

    [Fact]
    public async Task ExecuteAsync_CancelledToken_Throws()
    {
        var useCase = new RunDiagnostics([new FakeDiagnosticRule()], new FakeTimeProvider(_now));
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() => useCase.ExecuteAsync(new RunDiagnosticsRequest(_profile), cts.Token));
    }
}
