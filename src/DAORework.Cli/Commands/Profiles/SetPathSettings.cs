using System.ComponentModel;
using DAORework.Core.Profiles;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DAORework.Cli.Commands.Profiles;

public sealed class SetPathSettings : CommandSettings
{
    [CommandArgument(0, "<profile>")]
    [Description("Profile name.")]
    public string Profile { get; init; } = string.Empty;

    [CommandArgument(1, "<kind>")]
    [Description("install, game, launcher or documents.")]
    public string Kind { get; init; } = string.Empty;

    [CommandArgument(2, "<path>")]
    [Description("The path to store. Quote it if it contains spaces.")]
    public string Path { get; init; } = string.Empty;

    public ProfilePathKind ParsedKind => TryParseKind(Kind) ?? throw new InvalidOperationException("Validate() runs first.");

    public override ValidationResult Validate() =>
        TryParseKind(Kind) is null
            ? ValidationResult.Error($"Invalid path type: {Kind}. Valid values: install, game, launcher, documents")
            : ValidationResult.Success();

    private static ProfilePathKind? TryParseKind(string kind) => kind.ToLowerInvariant() switch
    {
        "install" or "root" or "install-directory" => ProfilePathKind.Install,
        "game" or "game-exe" or "daorigins" => ProfilePathKind.Game,
        "launcher" or "official-launcher" => ProfilePathKind.Launcher,
        "documents" or "docs" or "user-documents" => ProfilePathKind.Documents,
        _ => null,
    };
}
