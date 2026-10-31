using System.Diagnostics;
using System.IO.Abstractions;

namespace DAORework.FaultInjection.Tests.Host;

/// <summary>
/// The real file system, except that every call that can change the disk is counted, and immediately before call
/// number <c>killAt</c> the process kills itself: no <c>finally</c> block runs and nothing buffered is flushed (ADR-0014).
/// </summary>
internal sealed class KillingFileSystem : IFileSystem
{
    public const string KillPrefix = "KILLED BEFORE ";

    private readonly IFileSystem _inner;
    private readonly int _killAt;
    private int _writes;

    public KillingFileSystem(IFileSystem inner, int killAt)
    {
        _inner = inner;
        _killAt = killAt;
        File = FileSystemInterceptor<IFile>.Create(inner.File, this);
        Directory = FileSystemInterceptor<IDirectory>.Create(inner.Directory, this);
        FileStream = FileSystemInterceptor<IFileStreamFactory>.Create(inner.FileStream, this);
    }

    public int Writes => _writes;

    public IFile File { get; }

    public IDirectory Directory { get; }

    public IFileStreamFactory FileStream { get; }

    public IDirectoryInfoFactory DirectoryInfo => _inner.DirectoryInfo;

    public IDriveInfoFactory DriveInfo => _inner.DriveInfo;

    public IFileInfoFactory FileInfo => _inner.FileInfo;

    public IFileSystemWatcherFactory FileSystemWatcher => _inner.FileSystemWatcher;

    public IFileVersionInfoFactory FileVersionInfo => _inner.FileVersionInfo;

    public IPath Path => _inner.Path;

    public IRandomAccess RandomAccess => _inner.RandomAccess;

    /// <summary>Called before each write; kills the process when it is the one asked for.</summary>
    public void BeforeWrite(string what)
    {
        if (Interlocked.Increment(ref _writes) - 1 != _killAt)
        {
            return;
        }

        Console.Error.WriteLine($"{KillPrefix}{_killAt}: {what}");
        Process.GetCurrentProcess().Kill();
        Thread.Sleep(Timeout.Infinite);
    }
}
