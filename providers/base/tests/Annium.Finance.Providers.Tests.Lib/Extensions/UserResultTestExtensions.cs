using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.Operations;
using Annium.Testing;

namespace Annium.Finance.Providers.Tests.Lib.Extensions;

public static class UserResultTestExtensions
{
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

    public static async Task UnwrapAsync(this Task<UserResult> task)
    {
#pragma warning disable VSTHRD003
        var result = await task;
#pragma warning restore VSTHRD003

        result.Message.Is(string.Empty);
        result.Status.Is(UserOperationStatus.Ok);
    }

    public static T Unwrap<T>(this UserResult<T> result)
        where T : class
    {
        result.Message.Is(string.Empty);
        result.Status.Is(UserOperationStatus.Ok);

        var data = result.Data;
        data.IsNotDefault();

        return data;
    }

    public static void Unwrap(this UserResult result)
    {
        result.Message.Is(string.Empty);
        result.Status.Is(UserOperationStatus.Ok);
    }

    public static async Task EnsureFailedAsync<T>(this Task<UserResult<T>> task)
    {
#pragma warning disable VSTHRD003
        var result = await task;
#pragma warning restore VSTHRD003

        result.Message.IsNot(string.Empty);
        result.Status.IsNot(UserOperationStatus.Ok);
    }

    public static async Task EnsureFailedAsync(this Task<UserResult> task)
    {
#pragma warning disable VSTHRD003
        var result = await task;
#pragma warning restore VSTHRD003

        result.Message.IsNot(string.Empty);
        result.Status.IsNot(UserOperationStatus.Ok);
    }

    public static void EnsureFailed<T>(this UserResult<T> result)
    {
        result.Message.IsNot(string.Empty);
        result.Status.IsNot(UserOperationStatus.Ok);
    }

    public static void EnsureFailed(this UserResult result)
    {
        result.Message.IsNullOrWhiteSpace().IsFalse();
        result.Status.IsNot(UserOperationStatus.Ok);
    }
}
