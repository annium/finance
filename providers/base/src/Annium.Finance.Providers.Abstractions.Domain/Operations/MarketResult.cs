using System.Diagnostics.CodeAnalysis;

namespace Annium.Finance.Providers.Abstractions.Domain.Operations;

public sealed record MarketResult
{
    public static MarketResult Ok() => new(MarketOperationStatus.Ok, string.Empty);

    public static MarketResult New(MarketOperationStatus status) => new(status, string.Empty);

    public static MarketResult New(MarketOperationStatus status, string message) => new(status, message);

    public static MarketResult<T> Ok<T>(T data)
        where T : notnull => new(MarketOperationStatus.Ok, data, string.Empty);

    public static MarketResult<T> New<T>(MarketOperationStatus status, T data)
        where T : notnull => new(status, data, string.Empty);

    public static MarketResult<T> New<T>(MarketOperationStatus status, T data, string error)
        where T : notnull => new(status, data, error);

    public static MarketResult<T> From<T>(MarketResult result, T data)
        where T : notnull => new(result.Status, data, result.Message);

    public bool IsSuccess { get; }
    public bool IsFailure { get; }
    public MarketOperationStatus Status { get; }
    public string Message { get; }

    private MarketResult(MarketOperationStatus status, string message)
    {
        IsSuccess = status is MarketOperationStatus.Ok;
        IsFailure = !IsSuccess;
        Status = status;
        Message = message;
    }

    public override string ToString() => $"{Status} ({Message})";
}

public sealed record MarketResult<T>
{
    [MemberNotNullWhen(true, nameof(Data))]
    public bool IsSuccess { get; }

    [MemberNotNullWhen(false, nameof(Data))]
    public bool IsFailure { get; }
    public MarketOperationStatus Status { get; }
    public T? Data { get; }
    public string Message { get; }

    internal MarketResult(MarketOperationStatus status, T? data, string message)
    {
        IsSuccess = status is MarketOperationStatus.Ok;
        IsFailure = !IsSuccess;
        Status = status;
        Data = data;
        Message = message;
    }

    public override string ToString() => $"{Status} ({Message})";
}
