using System.Net;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.Operations;
using Annium.Finance.Providers.Crypto.Binance.Base.Contracts.Shared.Domain;
using Annium.Finance.Providers.Shared.Connectors.Extensions;
using Annium.Net.Http;
using OneOf;
using static Annium.Finance.Providers.Crypto.Binance.Base.Connectors.Extensions.HttpRequestHelper;

namespace Annium.Finance.Providers.Crypto.Binance.Base.Internal.Connectors.Extensions;

internal static class HttpRequestUserResultExtensions
{
    public static Task<UserResult<T?>> AsUserResultAsync<T>(this IHttpRequest request)
        where T : class
    {
        return request.AsUserResultAsync<T, OperationResult>(GetFailureAsync, MapResponse);
    }

    private static UserResult<T?> MapResponse<T>(IHttpResponse<OneOf<T, OperationResult>> response)
    {
        if (response.Data.IsT1)
        {
            var error = response.Data.AsT1;
            var status = MapOperationCode(error.Code);

            return UserResult.New<T?>(status, default, error.Message);
        }

        var data = response.Data.AsT0;

        if (response.IsSuccess)
            return UserResult.Ok<T?>(data);

        {
            var status = MapStatusCode(response.StatusCode);
            return UserResult.New<T?>(status, data);
        }
    }

    private static UserOperationStatus MapOperationCode(long code) =>
        code switch
        {
            OperationResult.Aborted => UserOperationStatus.Aborted,
            OperationResult.ParseError => UserOperationStatus.ParseError,
            < 0 => UserOperationStatus.BadRequest,
            _ => UserOperationStatus.UnknownError,
        };

    private static UserOperationStatus MapStatusCode(HttpStatusCode code) =>
        code switch
        {
            (HttpStatusCode)418 or HttpStatusCode.TooManyRequests => UserOperationStatus.TooManyRequests,
            HttpStatusCode.BadRequest => UserOperationStatus.BadRequest,
            _ => UserOperationStatus.UnknownError,
        };
}
