// OpenTweak - PC Game Optimization Tool
// Copyright 2024-2025 OpenTweak Contributors
// Licensed under PolyForm Shield License 1.0.0
// See LICENSE.md for full terms.

namespace OpenTweak.Common;

/// <summary>
/// Represents the result of an operation that can succeed or fail.
/// Use this instead of null returns or exceptions for expected failure cases.
/// </summary>
/// <typeparam name="T">The type of the success value.</typeparam>
public readonly struct Result<T>
{
    private readonly T? _value;
    private readonly string? _error;
    private readonly bool _isSuccess;

    private Result(T value)
    {
        _value = value;
        _error = null;
        _isSuccess = true;
    }

    private Result(string error)
    {
        _value = default;
        _error = error;
        _isSuccess = false;
    }

    /// <summary>
    /// Whether the operation succeeded.
    /// </summary>
    public bool IsSuccess => _isSuccess;

    /// <summary>
    /// Whether the operation failed.
    /// </summary>
    public bool IsFailure => !_isSuccess;

    /// <summary>
    /// The success value. Throws if the operation failed.
    /// </summary>
    public T Value => _isSuccess
        ? _value!
        : throw new InvalidOperationException($"Cannot access Value on failed result: {_error}");

    /// <summary>
    /// The error message. Throws if the operation succeeded.
    /// </summary>
    public string Error => !_isSuccess
        ? _error!
        : throw new InvalidOperationException("Cannot access Error on successful result");

    /// <summary>
    /// Gets the value or a default if the operation failed.
    /// </summary>
    public T? ValueOrDefault => _value;

    /// <summary>
    /// Creates a successful result with the given value.
    /// </summary>
    public static Result<T> Success(T value) => new(value);

    /// <summary>
    /// Creates a failed result with the given error message.
    /// </summary>
    public static Result<T> Failure(string error) => new(error);

    /// <summary>
    /// Creates a failed result from an exception.
    /// </summary>
    public static Result<T> FromException(Exception ex) => new(ex.Message);

    /// <summary>
    /// Implicitly converts a value to a successful result.
    /// </summary>
    public static implicit operator Result<T>(T value) => Success(value);

    /// <summary>
    /// Maps the value if successful, otherwise propagates the error.
    /// </summary>
    public Result<TNew> Map<TNew>(Func<T, TNew> mapper)
    {
        return _isSuccess
            ? Result<TNew>.Success(mapper(_value!))
            : Result<TNew>.Failure(_error!);
    }

    /// <summary>
    /// Executes an action if successful.
    /// </summary>
    public Result<T> OnSuccess(Action<T> action)
    {
        if (_isSuccess)
            action(_value!);
        return this;
    }

    /// <summary>
    /// Executes an action if failed.
    /// </summary>
    public Result<T> OnFailure(Action<string> action)
    {
        if (!_isSuccess)
            action(_error!);
        return this;
    }

    public override string ToString()
    {
        return _isSuccess ? $"Success({_value})" : $"Failure({_error})";
    }
}

/// <summary>
/// Non-generic Result for operations that don't return a value.
/// </summary>
public readonly struct Result
{
    private readonly string? _error;
    private readonly bool _isSuccess;

    private Result(bool isSuccess, string? error = null)
    {
        _isSuccess = isSuccess;
        _error = error;
    }

    /// <summary>
    /// Whether the operation succeeded.
    /// </summary>
    public bool IsSuccess => _isSuccess;

    /// <summary>
    /// Whether the operation failed.
    /// </summary>
    public bool IsFailure => !_isSuccess;

    /// <summary>
    /// The error message. Throws if the operation succeeded.
    /// </summary>
    public string Error => !_isSuccess
        ? _error!
        : throw new InvalidOperationException("Cannot access Error on successful result");

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static Result Success() => new(true);

    /// <summary>
    /// Creates a failed result with the given error message.
    /// </summary>
    public static Result Failure(string error) => new(false, error);

    /// <summary>
    /// Creates a failed result from an exception.
    /// </summary>
    public static Result FromException(Exception ex) => new(false, ex.Message);

    /// <summary>
    /// Executes an action if successful.
    /// </summary>
    public Result OnSuccess(Action action)
    {
        if (_isSuccess)
            action();
        return this;
    }

    /// <summary>
    /// Executes an action if failed.
    /// </summary>
    public Result OnFailure(Action<string> action)
    {
        if (!_isSuccess)
            action(_error!);
        return this;
    }

    public override string ToString()
    {
        return _isSuccess ? "Success" : $"Failure({_error})";
    }
}
