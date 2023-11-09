using System;
using System.Net;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.Operations;
using Annium.Net.Http;

namespace Annium.Finance.Providers.Shared.Connectors.Extensions;

public static class HttpRequestUserResultExtensions
{
    public static async Task<UserResult<TData>> AsUserResultAsync<TData, TError>(
        this IHttpRequest request,
        TData defaultValue,
        Func<TError, string> getError
    )
    {
        var response = await request.AsResponseAsync<TData, TError>(defaultValue);

        return response.Data.Match(
            UserResult.Ok,
            error =>
            {
                var operationStatus = MapHttpStatusCodeToOperationStatus(response.StatusCode);
                var errorMessage = getError(error);

                return UserResult.New(operationStatus, defaultValue, errorMessage);
            }
        );
    }

    private static UserOperationStatus MapHttpStatusCodeToOperationStatus(HttpStatusCode code) =>
        code switch
        {
            HttpStatusCode.BadRequest => UserOperationStatus.BadRequest,
            HttpStatusCode.Unauthorized => UserOperationStatus.Forbidden,
            HttpStatusCode.NotFound => UserOperationStatus.Forbidden,
            _ => UserOperationStatus.UncaughtError
        };
}
