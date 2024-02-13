using System;
using System.Net;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.Operations;
using Annium.Net.Http;

namespace Annium.Finance.Providers.Shared.Connectors.Extensions;

public static class HttpRequestUserResultExtensions
{
    public static async Task<UserResult<TData?>> AsUserResultAsync<TData, TError>(
        this IHttpRequest request,
        TError defaultError,
        Func<TError, UserOperationStatus?> getErrorStatus,
        Func<TError, string> getError
    )
        where TData : class
    {
        var response = await request.AsResponseAsync<TData, TError>(defaultError);

        return response.Data.Match<UserResult<TData?>>(
            UserResult.Ok!,
            error =>
            {
                var operationStatus = getErrorStatus(error) ?? MapHttpStatusCodeToOperationStatus(response.StatusCode);
                var errorMessage = getError(error);

                return UserResult.New<TData?>(operationStatus, null, errorMessage);
            }
        );
    }

    private static UserOperationStatus MapHttpStatusCodeToOperationStatus(HttpStatusCode code) =>
        code switch
        {
            HttpStatusCode.BadRequest => UserOperationStatus.BadRequest,
            HttpStatusCode.Unauthorized => UserOperationStatus.Forbidden,
            HttpStatusCode.NotFound => UserOperationStatus.Forbidden,
            _ => UserOperationStatus.UnknownError
        };
}
