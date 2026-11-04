using System.IO.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Adoption;

namespace DAORework.Application.Mods.Adoption;

/// <summary>
/// Reads the install folder's override (ROADMAP P2.5-22), <c>&lt;install&gt;\packages\core\override</c>, where older
/// guides put mods and which the game loads too. Only reads. No install folder, or no override in it, is simply empty.
/// </summary>
public sealed class ReadInstallOverride
{
    private readonly IFileSystem _fileSystem;

    public ReadInstallOverride(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public Result<InstallOverrideContents> Execute(string? installDirectory)
    {
        if (string.IsNullOrWhiteSpace(installDirectory))
        {
            return Result.Success(InstallOverrideContents.Empty);
        }

        var folder = GamePaths.OverrideDirectory(installDirectory);
        try
        {
            return Result.Success(_fileSystem.Directory.Exists(folder) ? InstallOverrideContents.From(Files(folder)) : InstallOverrideContents.Empty);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return Result.Failure<InstallOverrideContents>(new Error(
                DiagnosticCodes.InstallOverrideUnreadable,
                $"Couldn't read the override folder in your game folder: {exception.Message}",
                "Mods there aren't listed or compared until it can be read."));
        }
    }

    private List<InstallOverrideFile> Files(string folder) =>
        [.. _fileSystem.DirectoryInfo.New(folder).EnumerateFiles("*", SearchOption.AllDirectories)
            .Select(file => new InstallOverrideFile(_fileSystem.Path.GetRelativePath(folder, file.FullName).Replace('\\', '/'), file.Length))];
}
