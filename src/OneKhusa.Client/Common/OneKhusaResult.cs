using System.Diagnostics.CodeAnalysis;

namespace OneKhusa.Client.Common;

/// <summary>
/// Either a value or an <see cref="OneKhusaError"/>. API and network failures are returned rather
/// than thrown, because a declined or invalid request is a normal outcome for a payment call.
/// </summary>
public sealed class OneKhusaResult<T>
{
    private OneKhusaResult(T? value, OneKhusaError? error)
    {
        Value = value;
        Error = error;
    }

    public T? Value { get; }

    public OneKhusaError? Error { get; }

    [MemberNotNullWhen(true, nameof(Value))]
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess => Error is null;

    public static OneKhusaResult<T> Success(T value) => new(value, null);

    public static OneKhusaResult<T> Failure(OneKhusaError error) => new(default, error);
}
