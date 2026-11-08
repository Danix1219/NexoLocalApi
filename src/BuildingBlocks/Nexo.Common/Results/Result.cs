namespace Nexo.Common.Results;

/// <summary>Resultado explicito: exito con valor o fallo con codigo; nunca ambos.</summary>
public sealed class Result<T>
{
    private Result(T? value, NexoError? error)
    {
        Value = value;
        Error = error;
    }

    public bool IsSuccess => Error is null;

    public bool IsFailure => !IsSuccess;

    public T? Value { get; }

    public NexoError? Error { get; }

    public static Result<T> Success(T value) => new(value, null);

    public static Result<T> Failure(string code, string message) =>
        new(default, new NexoError(code, message));

    public static Result<T> Failure(NexoError error) => new(default, error);

    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<NexoError, TOut> onFailure) =>
        IsSuccess ? onSuccess(Value!) : onFailure(Error!);
}
