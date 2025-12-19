using System.Diagnostics.CodeAnalysis;
using Annium.Finance.Providers.Abstractions.Domain.Shared;
using Annium.Finance.Providers.Abstractions.Domain.Shared.Operations;

namespace Annium.Finance.Providers.Abstractions.Domain.Market.Operations;

public sealed record MarketResult : IBaseResult
{
    public static MarketResult Ok() => new(MarketOperationStatus.Ok, string.Empty);

    public static MarketResult New(MarketOperationStatus status) => new(status, string.Empty);

    public static MarketResult New(MarketOperationStatus status, string message) => new(status, message);

    public static MarketResult From<T>(MarketResult<T> result) => new(result.Status, result.Message);

    public static MarketResult<T> Ok<T>(T data) => new(MarketOperationStatus.Ok, data, string.Empty);

    public static MarketResult<T> New<T>(MarketOperationStatus status, T data) => new(status, data, string.Empty);

    public static MarketResult<T> New<T>(MarketOperationStatus status, T data, string error) =>
        new(status, data, error);

    public static MarketResult<T> From<T>(MarketResult result, T data) => new(result.Status, data, result.Message);

    public static MarketResult<T> From<TSource, T>(MarketResult<TSource> result, T data) =>
        new(result.Status, data, result.Message);

    public bool IsNetworkError { get; }

    public bool IsAborted { get; }

    public bool IsSuccess { get; }

    public bool IsFailure { get; }

    public MarketOperationStatus Status { get; }

    public string Message { get; }

    private MarketResult(MarketOperationStatus status, string message)
    {
        IsNetworkError = status is MarketOperationStatus.NetworkError;
        IsAborted = status is MarketOperationStatus.Aborted;
        IsSuccess = status is MarketOperationStatus.Ok;
        IsFailure = !IsNetworkError && !IsAborted && !IsSuccess;
        Status = status;
        Message = message;
    }

    public override string ToString() => $"{Status} ({Message})";
}

public sealed record MarketResult<T> : IBaseResult<T>
{
    public bool IsNetworkError { get; }

    public bool IsAborted { get; }

    [MemberNotNullWhen(true, nameof(Data))]
    public bool IsSuccess { get; }

    public bool IsFailure { get; }

    public MarketOperationStatus Status { get; }

    public T? Data { get; }

    public string Message { get; }

    internal MarketResult(MarketOperationStatus status, T? data, string message)
    {
        IsNetworkError = status is MarketOperationStatus.NetworkError;
        IsAborted = status is MarketOperationStatus.Aborted;
        IsSuccess = status is MarketOperationStatus.Ok;
        IsFailure = !IsNetworkError && !IsAborted && !IsSuccess;
        Status = status;
        Data = data;
        Message = message;
    }

    public override string ToString() => $"{Status} ({Message})";
}
