using System.IO.Abstractions;

namespace DAORework.FaultInjection.Tests.Host;

/// <summary>A stream opened for writing whose every write is also a point where the process can be killed.</summary>
internal sealed class KillingStream : FileSystemStream
{
    private readonly KillingFileSystem _fileSystem;
    private readonly string _name;

    public KillingStream(FileSystemStream inner, KillingFileSystem fileSystem)
        : base(inner, inner.Name, inner.IsAsync)
    {
        _fileSystem = fileSystem;
        _name = inner.Name;
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        _fileSystem.BeforeWrite($"write {_name}");
        base.Write(buffer, offset, count);
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        _fileSystem.BeforeWrite($"write {_name}");
        base.Write(buffer);
    }

    public override void WriteByte(byte value)
    {
        _fileSystem.BeforeWrite($"write {_name}");
        base.WriteByte(value);
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        _fileSystem.BeforeWrite($"write {_name}");
        return base.WriteAsync(buffer, offset, count, cancellationToken);
    }

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        _fileSystem.BeforeWrite($"write {_name}");
        return base.WriteAsync(buffer, cancellationToken);
    }
}
