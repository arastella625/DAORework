using DAORework.Core.Common;
using Xunit;

namespace DAORework.Core.Tests.Common;

public sealed class ResultTests
{
    private static readonly Error _error = new("DAO_TEST_FAILED", "It failed.", "Try again.");

    [Fact]
    public void Success_HasNoError()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Failure_ExposesTheError()
    {
        var result = Result.Failure(_error);

        Assert.True(result.IsFailure);
        Assert.Same(_error, result.Error);
    }

    [Fact]
    public void SuccessOfT_ExposesTheValue()
    {
        var result = Result.Success(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void FailureOfT_ReadingValue_Throws()
    {
        var result = Result.Failure<int>(_error);

        var exception = Assert.Throws<InvalidOperationException>(() => result.Value);
        Assert.Contains(_error.Code, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TryGetValue_Success_ReturnsTrueWithValue()
    {
        var found = Result.Success("x").TryGetValue(out var value, out var error);

        Assert.True(found);
        Assert.Equal("x", value);
        Assert.Null(error);
    }

    [Fact]
    public void TryGetValue_Failure_ReturnsFalseWithError()
    {
        var found = Result.Failure<string>(_error).TryGetValue(out var value, out var error);

        Assert.False(found);
        Assert.Null(value);
        Assert.Same(_error, error);
    }

    [Fact]
    public void Match_DispatchesOnOutcome()
    {
        Assert.Equal("ok:5", Result.Success(5).Match(v => $"ok:{v}", e => $"err:{e.Code}"));
        Assert.Equal("err:DAO_TEST_FAILED", Result.Failure<int>(_error).Match(v => $"ok:{v}", e => $"err:{e.Code}"));
    }

    [Fact]
    public void Failure_NullError_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => Result.Failure(null!));
        Assert.Throws<ArgumentNullException>(() => Result.Failure<int>(null!));
    }
}
