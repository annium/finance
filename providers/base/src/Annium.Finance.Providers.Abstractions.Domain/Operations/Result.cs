using System;
using System.Threading.Tasks;

namespace Annium.Finance.Providers.Abstractions.Domain.Operations;

public sealed record Result<TS, TD> : Result<TS>
{
    public TD Data { get; }

    public Result(TS status, TD data, string error)
        : base(status, error)
    {
        Data = data;
    }
}

public record Result<TS>
{
    public TS Status { get; }
    public string Error { get; }
    public bool IsSuccess { get; }
    public bool IsFailure { get; }

    public Result(TS status, string error)
    {
        if (error != string.Empty && string.IsNullOrWhiteSpace(error))
            throw new ArgumentException($"Error '{error}' is null or white space");

        Status = status;
        Error = error;
        IsSuccess = string.IsNullOrWhiteSpace(Error);
        IsFailure = !IsSuccess;
    }

    public override string ToString() => IsSuccess ? $"{Status}" : $"{Status}: {Error}";
}

public static class Result
{
    public static Result<MarketOperationStatus, T> Market<T>(T data) =>
        new(MarketOperationStatus.Ok, data, string.Empty);

    public static Result<MarketOperationStatus, T> Market<T>(MarketOperationStatus status, T data, string error) =>
        new(status, data, error);

    public static Result<MarketOperationStatus> Market() => new(MarketOperationStatus.Ok, string.Empty);

    public static Result<MarketOperationStatus> Market(MarketOperationStatus status, string error) =>
        new(status, error);

    public static Result<UserOperationStatus, T> User<T>(T data) => new(UserOperationStatus.Ok, data, string.Empty);

    public static Result<UserOperationStatus, T> User<T>(UserOperationStatus status, T data, string error) =>
        new(status, data, error);

    public static Result<UserOperationStatus> User() => new(UserOperationStatus.Ok, string.Empty);

    public static Result<UserOperationStatus> User(UserOperationStatus status, string error) => new(status, error);

    public static async Task<Result<MarketOperationStatus, T>> AsMarketAsync<T>(
        this Task<Result<UserOperationStatus, T>> task
    )
    {
        var result = await task;

        return Market(MapStatus(result.Status), result.Data, result.Error);
    }

    public static async Task<Result<MarketOperationStatus>> AsMarketAsync(this Task<Result<UserOperationStatus>> task)
    {
        var result = await task;

        return Market(MapStatus(result.Status), result.Error);
    }

    private static MarketOperationStatus MapStatus(UserOperationStatus x) =>
        x switch
        {
            UserOperationStatus.NetworkError => MarketOperationStatus.NetworkError,
            UserOperationStatus.BadRequest => MarketOperationStatus.BadRequest,
            UserOperationStatus.NotFound => MarketOperationStatus.NotFound,
            UserOperationStatus.Ok => MarketOperationStatus.Ok,
            UserOperationStatus.ParseError => MarketOperationStatus.ParseError,
            UserOperationStatus.UncaughtError => MarketOperationStatus.UncaughtError,
            _
                => throw new InvalidOperationException(
                    $"{nameof(UserOperationStatus)} {x} is not mapped to {nameof(MarketOperationStatus)}"
                )
        };
};
