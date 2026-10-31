using System.IO.Abstractions;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace DAORework.FaultInjection.Tests.Host;

/// <summary>
/// Forwards every call on <typeparamref name="T"/> to the real implementation, telling <see cref="KillingFileSystem"/>
/// first when the call can change the disk. Any method added to the abstractions later is covered without a change here.
/// </summary>
public class FileSystemInterceptor<T> : DispatchProxy
    where T : class
{
    private static readonly string[] _changingPrefixes = ["Append", "Copy", "Create", "Decrypt", "Delete", "Encrypt", "Move", "Replace", "Set", "Write"];

    private T _target = null!;
    private KillingFileSystem _fileSystem = null!;

    internal static T Create(T target, KillingFileSystem fileSystem)
    {
        var proxy = Create<T, FileSystemInterceptor<T>>();
        var interceptor = (FileSystemInterceptor<T>)(object)proxy;
        interceptor._target = target;
        interceptor._fileSystem = fileSystem;
        return proxy;
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        ArgumentNullException.ThrowIfNull(targetMethod);
        var arguments = args ?? [];
        var writes = ChangesTheDisk(targetMethod.Name, arguments);
        if (writes)
        {
            _fileSystem.BeforeWrite($"{typeof(T).Name}.{targetMethod.Name}({arguments.FirstOrDefault()})");
        }

        try
        {
            var result = targetMethod.Invoke(_target, arguments);
            return writes && result is FileSystemStream stream ? new KillingStream(stream, _fileSystem) : result;
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }

    /// <summary>A stream opened for writing, or a method whose name says it changes the disk.</summary>
    private static bool ChangesTheDisk(string name, object?[] arguments)
    {
        if (name is "New" or "Open" or "OpenHandle" or "Wrap")
        {
            return OpensForWriting(arguments);
        }

        return name == "OpenWrite" || (name != "SetCurrentDirectory" && _changingPrefixes.Any(p => name.StartsWith(p, StringComparison.Ordinal)));
    }

    /// <summary>A stream opened with write access; with only a mode, .NET opens for reading and writing.</summary>
    private static bool OpensForWriting(object?[] arguments)
    {
        foreach (var argument in arguments)
        {
            switch (argument)
            {
                case FileAccess access:
                    return access.HasFlag(FileAccess.Write);
                case FileStreamOptions options:
                    return options.Access.HasFlag(FileAccess.Write);
            }
        }

        return arguments.Any(a => a is FileMode);
    }
}
