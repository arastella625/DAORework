using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;

namespace DAORework.Application.Tests.Fakes;

/// <summary>A file system whose folders can't be listed once <see cref="Denied"/> is set, as a folder without permission.</summary>
internal sealed class DeniedFileSystem : MockFileSystem
{
    public bool Denied { get; set; }

    public override IDirectoryInfoFactory DirectoryInfo => Denied ? throw new UnauthorizedAccessException("denied") : base.DirectoryInfo;
}
