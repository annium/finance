using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.Operations;
using Annium.Testing;

namespace Annium.Finance.Providers.Tests.Shared.Extensions;

public static class UserResultTestExtensions
{
    public static async Task<T> Unwrap<T>(this Task<UserResult<T>> task)
        where T : class
    {
        var result = await task;

        result.Message.IsNullOrWhiteSpace().IsTrue();
        result.Status.Is(UserOperationStatus.Ok);

        var data = result.Data;
        data.IsNotDefault();

        return data;
    }

    public static async Task Unwrap(this Task<UserResult> task)
    {
        var result = await task;

        result.Message.IsNullOrWhiteSpace().IsTrue();
        result.Status.Is(UserOperationStatus.Ok);
    }

    public static T Unwrap<T>(this UserResult<T> result)
        where T : class
    {
        result.Message.IsNullOrWhiteSpace().IsTrue();
        result.Status.Is(UserOperationStatus.Ok);

        var data = result.Data;
        data.IsNotDefault();

        return data;
    }

    public static void Unwrap(this UserResult result)
    {
        result.Message.IsNullOrWhiteSpace().IsTrue();
        result.Status.Is(UserOperationStatus.Ok);
    }

    public static async Task EnsureFailed<T>(this Task<UserResult<T>> task)
    {
        var result = await task;

        result.Message.IsNullOrWhiteSpace().IsFalse();
        result.Status.IsNot(UserOperationStatus.Ok);
    }

    public static async Task EnsureFailed(this Task<UserResult> task)
    {
        var result = await task;

        result.Message.IsNullOrWhiteSpace().IsFalse();
        result.Status.IsNot(UserOperationStatus.Ok);
    }

    public static void EnsureFailed<T>(this UserResult<T> result)
    {
        result.Message.IsNullOrWhiteSpace().IsFalse();
        result.Status.IsNot(UserOperationStatus.Ok);
    }

    public static void EnsureFailed(this UserResult result)
    {
        result.Message.IsNullOrWhiteSpace().IsFalse();
        result.Status.IsNot(UserOperationStatus.Ok);
    }
}
