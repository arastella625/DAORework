using System.Diagnostics.CodeAnalysis;

namespace DAORework.Core.Common;

/// <summary>The outcome of an operation that yields a value or an expected failure (ADR-0004).</summary>
public sealed class Result<T>
{
    private readonly T? _value;

    internal Result(T? value, Error? error)
    {
        _value = value;
        Error = error;
    }

    public bool IsSuccess => Error is null;

    public bool IsFailure => Error is not null;

    public Error? Error { get; }

    /// <summary>Gets the value. Throws when the result is a failure; check <see cref="IsSuccess"/> or use <see cref="TryGetValue"/> first.</summary>
    public T Value
    {
        get
        {
            if (Error is not null)
            {
                throw new InvalidOperationException($"The result is a failure ({Error.Code}); it has no value.");
            }

            // A success result is only ever constructed with a value.
            return _value!;
        }
    }

    public bool TryGetValue([MaybeNullWhen(false)] out T value, [NotNullWhen(false)] out Error? error)
    {
        value = _value;
        error = Error;
        return Error is null;
    }

    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<Error, TOut> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);
        return Error is null ? onSuccess(Value) : onFailure(Error);
    }
}
