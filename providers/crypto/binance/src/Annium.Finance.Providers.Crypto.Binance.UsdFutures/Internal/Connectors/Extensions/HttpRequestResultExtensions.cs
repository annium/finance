using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.Operations;
using Annium.Finance.Providers.Crypto.Binance.Base.Contracts.Shared.Domain;
using Annium.Finance.Providers.Shared.Connectors.Extensions;
using Annium.Net.Http;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Connectors.Extensions;

public static class HttpRequestResultExtensions
{
    private static readonly OperationResult DefaultError = new(1, "unknown failure");

    public static Task<MarketResult<T?>> AsMarketResultAsync<T>(this IHttpRequest request)
        where T : class
    {
        return request.AsMarketResultAsync<T, OperationResult>(DefaultError, GetError);
    }

    public static Task<UserResult<T?>> AsUserResultAsync<T>(this IHttpRequest request)
        where T : class
    {
        return request.AsUserResultAsync<T, OperationResult>(DefaultError, GetErrorStatus, GetError);
    }

    private static UserOperationStatus? GetErrorStatus(OperationResult result) => null;

    private static string GetError(OperationResult result) => result.Message;
}
