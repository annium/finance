using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.User.Operations;
using Annium.Testing;

namespace Annium.Finance.Providers.Tests.Lib.User.Operations;

/// <summary>
/// Assertion helpers for <see cref="UserResult"/> and <see cref="UserResult{T}"/>: unwrap a successful
/// result into its data (failing the test if it wasn't successful), or assert that a result failed.
/// </summary>
public static class UserResultTestExtensions
{
    /// <summary>
    /// Awaits the task, asserts the result succeeded with an empty message, and returns its data.
    /// </summary>
    /// <typeparam name="T">The type of the result's data.</typeparam>
    /// <param name="task">The pending user result.</param>
    /// <returns>The result's data.</returns>
    public static async Task<T> UnwrapAsync<T>(this Task<UserResult<T?>> task)
        where T : class
    {
#pragma warning disable VSTHRD003
        var result = await task;
#pragma warning restore VSTHRD003

        result.Message.Is(string.Empty);
        result.Status.Is(UserOperationStatus.Ok);

        var data = result.Data;
        data.IsNotDefault();

        return data;
    }

    /// <summary>
    /// Awaits the task and asserts the result succeeded with an empty message.
    /// </summary>
    /// <param name="task">The pending user result.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task UnwrapAsync(this Task<UserResult> task)
    {
#pragma warning disable VSTHRD003
        var result = await task;
#pragma warning restore VSTHRD003

        result.Message.Is(string.Empty);
        result.Status.Is(UserOperationStatus.Ok);
    }

    /// <summary>
    /// Asserts the result succeeded with an empty message, and returns its data.
    /// </summary>
    /// <typeparam name="T">The type of the result's data.</typeparam>
    /// <param name="result">The user result.</param>
    /// <returns>The result's data.</returns>
    public static T Unwrap<T>(this UserResult<T> result)
        where T : class
    {
        result.Message.Is(string.Empty);
        result.Status.Is(UserOperationStatus.Ok);

        var data = result.Data;
        data.IsNotDefault();

        return data;
    }

    /// <summary>
    /// Asserts the result succeeded with an empty message.
    /// </summary>
    /// <param name="result">The user result.</param>
    public static void Unwrap(this UserResult result)
    {
        result.Message.Is(string.Empty);
        result.Status.Is(UserOperationStatus.Ok);
    }

    /// <summary>
    /// Awaits the task and asserts the result failed with a non-empty message.
    /// </summary>
    /// <typeparam name="T">The type of the result's data.</typeparam>
    /// <param name="task">The pending user result.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task EnsureFailedAsync<T>(this Task<UserResult<T>> task)
    {
#pragma warning disable VSTHRD003
        var result = await task;
#pragma warning restore VSTHRD003

        result.Message.IsNullOrWhiteSpace().IsFalse();
        result.Status.IsNot(UserOperationStatus.Ok);
    }

    /// <summary>
    /// Awaits the task and asserts the result failed with a non-empty message.
    /// </summary>
    /// <param name="task">The pending user result.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task EnsureFailedAsync(this Task<UserResult> task)
    {
#pragma warning disable VSTHRD003
        var result = await task;
#pragma warning restore VSTHRD003

        result.Message.IsNullOrWhiteSpace().IsFalse();
        result.Status.IsNot(UserOperationStatus.Ok);
    }

    /// <summary>
    /// Asserts the result failed with a non-empty message.
    /// </summary>
    /// <typeparam name="T">The type of the result's data.</typeparam>
    /// <param name="result">The user result.</param>
    public static void EnsureFailed<T>(this UserResult<T> result)
    {
        result.Message.IsNullOrWhiteSpace().IsFalse();
        result.Status.IsNot(UserOperationStatus.Ok);
    }

    /// <summary>
    /// Asserts the result failed with a non-empty message.
    /// </summary>
    /// <param name="result">The user result.</param>
    public static void EnsureFailed(this UserResult result)
    {
        result.Message.IsNullOrWhiteSpace().IsFalse();
        result.Status.IsNot(UserOperationStatus.Ok);
    }
}
