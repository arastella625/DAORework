using Avalonia.Data.Converters;
using DAORework.Core.Diagnostics;

namespace DAORework.App.Views;

/// <summary>Turns a diagnostic's severity into the class and icon its row shows (ADR-0016 rule 4).</summary>
public static class SeverityConverters
{
    public static FuncValueConverter<DiagnosticSeverity, bool> IsInfo { get; } = new(severity => severity == DiagnosticSeverity.Info);

    public static FuncValueConverter<DiagnosticSeverity, bool> IsWarning { get; } = new(severity => severity == DiagnosticSeverity.Warning);

    public static FuncValueConverter<DiagnosticSeverity, bool> IsError { get; } = new(severity => severity == DiagnosticSeverity.Error);
}
