using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.Market.Operations;
using Annium.Testing;

namespace Annium.Finance.Providers.Tests.Lib.Market.Operations;

/// <summary>
/// Assertion helpers for <see cref="MarketResult"/> and <see cref="MarketResult{T}"/>: unwrap a successful
/// result into its data (failing the test if it wasn't successful), or assert that a result failed.
/// </summary>
public static class MarketResultTestExtensions
{
    /// <summary>
    /// Awaits the task, asserts the result succeeded with an empty message, and returns its data.
    /// </summary>
    /// <typeparam name="T">The type of the result's data.</typeparam>
    /// <param name="task">The pending market result.</param>
    /// <returns>The result's data.</returns>
    public static async Task<T> UnwrapAsync<T>(this Task<MarketResult<T>> task)
        where T : class
    {
#pragma warning disable VSTHRD003
        var result = await task;
#pragma warning restore VSTHRD003

        result.Message.Is(string.Empty);
        result.Status.Is(MarketOperationStatus.Ok);

        var data = result.Data;
        data.IsNotDefault();

        return data;
    }

    /// <summary>
    /// Awaits the task and asserts the result succeeded with an empty message.
    /// </summary>
    /// <param name="task">The pending market result.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task UnwrapAsync(this Task<MarketResult> task)
    {
#pragma warning disable VSTHRD003
        var result = await task;
#pragma warning restore VSTHRD003

        result.Message.Is(string.Empty);
        result.Status.Is(MarketOperationStatus.Ok);
    }

    /// <summary>
    /// Asserts the result succeeded with an empty message, and returns its data.
    /// </summary>
    /// <typeparam name="T">The type of the result's data.</typeparam>
    /// <param name="result">The market result.</param>
    /// <returns>The result's data.</returns>
    public static T Unwrap<T>(this MarketResult<T> result)
        where T : class
    {
        result.Message.Is(string.Empty);
        result.Status.Is(MarketOperationStatus.Ok);

        var data = result.Data;
        data.IsNotDefault();

        return data;
    }

    /// <summary>
    /// Asserts the result succeeded with an empty message.
    /// </summary>
    /// <param name="result">The market result.</param>
    public static void Unwrap(this MarketResult result)
    {
        result.Message.Is(string.Empty);
        result.Status.Is(MarketOperationStatus.Ok);
    }

    /// <summary>
    /// Awaits the task and asserts the result failed with a non-empty message.
    /// </summary>
    /// <typeparam name="T">The type of the result's data.</typeparam>
    /// <param name="task">The pending market result.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task EnsureFailedAsync<T>(this Task<MarketResult<T>> task)
    {
#pragma warning disable VSTHRD003
        var result = await task;
#pragma warning restore VSTHRD003

        result.Message.IsNullOrWhiteSpace().IsFalse();
        result.Status.IsNot(MarketOperationStatus.Ok);
    }

    /// <summary>
    /// Awaits the task and asserts the result failed with a non-empty message.
    /// </summary>
    /// <param name="task">The pending market result.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task EnsureFailedAsync(this Task<MarketResult> task)
    {
#pragma warning disable VSTHRD003
        var result = await task;
#pragma warning restore VSTHRD003

        result.Message.IsNullOrWhiteSpace().IsFalse();
        result.Status.IsNot(MarketOperationStatus.Ok);
    }

    /// <summary>
    /// Asserts the result failed with a non-empty message.
    /// </summary>
    /// <typeparam name="T">The type of the result's data.</typeparam>
    /// <param name="result">The market result.</param>
    public static void EnsureFailed<T>(this MarketResult<T> result)
    {
        result.Message.IsNullOrWhiteSpace().IsFalse();
        result.Status.IsNot(MarketOperationStatus.Ok);
    }

    /// <summary>
    /// Asserts the result failed with a non-empty message.
    /// </summary>
    /// <param name="result">The market result.</param>
    public static void EnsureFailed(this MarketResult result)
    {
        result.Message.IsNullOrWhiteSpace().IsFalse();
        result.Status.IsNot(MarketOperationStatus.Ok);
    }
}
